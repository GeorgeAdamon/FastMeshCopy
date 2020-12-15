#define USE_UNSAFE // Comment this line to use the "safe" version of copying.

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnchartedLimbo.Tools.FastMeshCopy.Runtime
{
    /// <summary>
    /// Author: George Adamopoulos
    /// Version: 1.0.1
    /// Date: 2020-05-03
    /// License: GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
    /// </summary>
    public static class FastMeshCopyUtility
    {

        private const int SHORT_SIZE = 2;
        private const int INT_SIZE   = 4;
        private const int FLOAT_SIZE = 4;

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
                    UnsafeUtility.MemCpy(outData.GetUnsafePtr(), inData.GetUnsafeReadOnlyPtr(),
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
                    UnsafeUtility.MemCpy(outData.GetUnsafePtr(), inData.GetUnsafeReadOnlyPtr(), indexCount * indexSize);
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
