#define USE_UNSAFE // Comment this line to use the "safe" version of copying.

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityUnsafeUtility = Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using static UnchartedLimbo.Tools.FastMeshCopy.Runtime.Constants;

namespace UnchartedLimbo.Tools.FastMeshCopy.Runtime
{
    /// <summary>
    /// Author: George Adamopoulos
    /// Version: 2.0.0
    /// Date: 2020-05-03
    /// License: GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
    /// </summary>
    public static class FastMeshCopyUtility
    {
    #if UNITY_2020_1_OR_NEWER
        /// <summary>
        /// Attempts to copy the data of the current <see cref="Mesh"/> to another one, as fast as possible,
        /// with minimal allocations (a few tens of bytes in scenarios with very large meshes).
        /// </summary>
        public static void CopyTo(this Mesh inMesh, ref Mesh outMesh)
        {
            if (inMesh == null) return;

            if (outMesh == null)
            {
                outMesh = new Mesh();
            }
            else
            {
                outMesh.Clear();
            }

            outMesh.name   = inMesh.name;
            outMesh.bounds = inMesh.bounds;

            using (var readArray = Mesh.AcquireReadOnlyMeshData(inMesh))
            {
                //-------------------------------------------------------------
                // INPUT INFO
                //-------------------------------------------------------------
                var readData = readArray[0];

                // Formats
                var vertexFormat = inMesh.GetVertexAttributes();
                var indexFormat  = inMesh.indexFormat;
                var isIndexShort = indexFormat == IndexFormat.UInt16;

                // Counts
                var vertexCount = readData.vertexCount;
                var indexCount =
                        isIndexShort ? readData.GetIndexData<ushort>().Length : readData.GetIndexData<uint>().Length;

                // Element Size in bytes
                var indexSize  = isIndexShort ? SHORT_SIZE : INT_SIZE;
                var vertexSize = 0;

                for (var i = 0; i < vertexFormat.Length; i++)
                {
                    // 4 bytes per component by default
                    var size = FLOAT_SIZE;

                    switch (vertexFormat[i].format)
                    {
                        case VertexAttributeFormat.Float16:
                        case VertexAttributeFormat.UNorm16:
                        case VertexAttributeFormat.SNorm16:
                        case VertexAttributeFormat.UInt16:
                        case VertexAttributeFormat.SInt16:
                            size = 2;
                            break;
                        case VertexAttributeFormat.UNorm8:
                        case VertexAttributeFormat.SNorm8:
                        case VertexAttributeFormat.UInt8:
                        case VertexAttributeFormat.SInt8:
                            size = 1;
                            break;
                    }

                    vertexSize += vertexFormat[i].dimension * size;
                }


                //-------------------------------------------------------------
                // OUTPUT SETUP
                //-------------------------------------------------------------
                var writeArray = Mesh.AllocateWritableMeshData(1);
                var writeData  = writeArray[0];
                writeData.SetVertexBufferParams(vertexCount, vertexFormat);
                writeData.SetIndexBufferParams(indexCount, indexFormat);

                //-------------------------------------------------------------
                // MEMORY COPYING
                //-------------------------------------------------------------
                NativeArray<byte> inData;
                NativeArray<byte> outData;

                // Vertices
                inData  = readData.GetVertexData<byte>();
                outData = writeData.GetVertexData<byte>();

            #if USE_UNSAFE
                unsafe
                {
                    UnityUnsafeUtility.MemCpy(outData.GetUnsafePtr(), inData.GetUnsafeReadOnlyPtr(),
                                              vertexCount * vertexSize);
                }
            #else
            inData.CopyTo(outData);
            #endif


                // Indices
                inData  = readData.GetIndexData<byte>();
                outData = writeData.GetIndexData<byte>();

            #if USE_UNSAFE
                unsafe
                {
                    UnityUnsafeUtility.MemCpy(outData.GetUnsafePtr(), inData.GetUnsafeReadOnlyPtr(),
                                              indexCount * indexSize);
                }
            #else
            inData.CopyTo(outData);
            #endif

                //-------------------------------------------------------------
                // FINALIZATION
                //-------------------------------------------------------------
                writeData.subMeshCount = inMesh.subMeshCount;

                // Set all sub-meshes
                for (var i = 0; i < inMesh.subMeshCount; i++)
                {
                    writeData.SetSubMesh(i,
                                         new SubMeshDescriptor((int) inMesh.GetIndexStart(i),
                                                               (int) inMesh.GetIndexCount(i)));
                }


                Mesh.ApplyAndDisposeWritableMeshData(writeArray, outMesh);
            }
        }

        /// <summary>
        /// Combine transformed instances of a mesh.
        /// </summary>
        public static Mesh CopyReplicate(this Mesh mesh, NativeArray<float4x4> matrices)
        {
            using (var readArray = Mesh.AcquireReadOnlyMeshData(mesh))
            {
                var m = new Mesh
                {
                        subMeshCount = 1,
                        indexFormat  = IndexFormat.UInt32
                };

                //-------------------------------------------------------------
                // COLLECT ALL NECESSARY INPUT INFO
                //-------------------------------------------------------------
                // Source -----------------------------------------------------
                var readData = readArray[0];

                // Formats
                var sourceVertexSize  = mesh.SizeOfVertex();
                var sourceIndexFormat = mesh.indexFormat;

                // Counts
                var sourceVertexCount = readData.vertexCount;
                var sourceIndexCount  = readData.GetIndexCount();

                // Destination -----------------------------------------------------
                var destIndexFormat  = IndexFormat.UInt32;
                var destVertexFormat = mesh.CopyVertexFormat(0, 1);
                var destIndexCount   = sourceIndexCount  * matrices.Length;
                var destVertexCount  = sourceVertexCount * matrices.Length;

                var hasStream1 = !mesh.IsVertexPositionOnly();


                //-------------------------------------------------------------
                // OUTPUT SETUP
                //-------------------------------------------------------------
                var writeArray = Mesh.AllocateWritableMeshData(1);
                var writeData  = writeArray[0];

                writeData.SetVertexBufferParams(destVertexCount, destVertexFormat);
                writeData.SetIndexBufferParams(destIndexCount, destIndexFormat);

                //-------------------------------------------------------------
                // MEMORY COPYING
                //-------------------------------------------------------------
                // Replicate every other vertex attribute ---------------------
                // Essentially skip the first 12 bytes (= 3 floats) of every vertex,
                // because we know they represent position, and we handled this above.
                // Everything that is not VertexPosition will be written to stream 1 !
                unsafe
                {
                    if (hasStream1)
                    {
                        var inData  = readData.GetVertexData<byte>();
                        var outData = writeData.GetVertexData<byte>(1); // Notice that we write to stream 1!

                        var destElementSize = sourceVertexSize - FLOAT3_SIZE;
                        var source =
                                FLOAT3_SIZE +
                                (byte*) inData.GetUnsafeReadOnlyPtr(); // Begin after the first vertex = first 12 bytes
                        var copies = matrices.Length;

                        var noPosition =
                                new NativeArray<byte>(destElementSize * readData.vertexCount, Allocator.TempJob);

                        // REMOVE POSITIONS FROM ORIGINAL MESH STREAM
                        Unity.Collections.LowLevel.Unsafe.UnsafeUtility
                             .MemCpyStride(destination: noPosition.GetUnsafePtr(),
                                           destinationStride: destElementSize,
                                           source: source,
                                           sourceStride: sourceVertexSize,
                                           elementSize: destElementSize,
                                           count: readData.vertexCount);

                        // REPLICATE NORMALS,COLORS,UV ETC INTO THE MERGED MESH
                        UnsafeUtility.MemCpyReplicate(destination: outData, source: noPosition, count: copies);

                        noPosition.Dispose();
                    }


                    // Transform Vertices ----------------------------------------
                    var inVertices  = new NativeArray<Vector3>(sourceVertexCount, Allocator.TempJob);
                    var outVertices = writeData.GetVertexData<float3>(0);

                    readData.GetVertices(inVertices);

                    new HelperJobs.TransformVerticesJob
                    {
                            inputVertices  = inVertices.Reinterpret<float3>(),
                            matrices       = matrices,
                            outputVertices = outVertices
                    }.Schedule(destVertexCount, 128).Complete();


                    //Indices ---------------------------------------------------
                    var inData2  = readData.GetIndexData<byte>().GetUnsafeReadOnlyPtr();
                    var outData2 = writeData.GetIndexData<int>();

                    if (sourceIndexFormat == IndexFormat.UInt16)
                    {
                        new HelperJobs.OffsetReplicateIndicesJob<ushort>
                        {
                                inputIndices        = inData2,
                                outputIndices       = outData2,
                                originalVertexCount = sourceVertexCount,
                                originalIndexCount  = sourceIndexCount
                        }.Schedule(destIndexCount, 128).Complete();
                    }
                    else
                    {
                        new HelperJobs.OffsetReplicateIndicesJob<uint>
                        {
                                inputIndices        = inData2,
                                outputIndices       = outData2,
                                originalVertexCount = sourceVertexCount,
                                originalIndexCount  = sourceIndexCount
                        }.Schedule(destIndexCount, 128).Complete();
                    }

                    inVertices.Dispose();
                }

                writeData.subMeshCount = 1;
                writeData.SetSubMesh(0, new SubMeshDescriptor(0, destIndexCount, mesh.GetTopology(0)));
                Mesh.ApplyAndDisposeWritableMeshData(writeArray, m);
                m.RecalculateBounds();

                return m;
            }

        #else
            /// <summary>
            /// Attempts to copy the data of the current <see cref="Mesh"/> to another one, as fast as possible,
            /// with minimal allocations (a few tens of bytes in scenarios with very large meshes).
            /// </summary>
            public static void CopyTo(this Mesh inMesh, ref Mesh outMesh)
            {
                    // PRE-UNITY 2020 WAY OF COPYING
                    outMesh.indexFormat = inMesh.indexFormat;
                    outMesh.SetVertices(inMesh.vertices);
                    outMesh.SetNormals(inMesh.normals);
                    outMesh.SetUVs(0,inMesh.uv);
                    outMesh.SetIndices(inMesh.GetIndices(0), inMesh.GetTopology(0), 0);
                    outMesh.SetColors(inMesh.colors);
            }
        #endif
        }
    }
}