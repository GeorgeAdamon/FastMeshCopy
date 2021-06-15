using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnchartedLimbo.Tools.FastMeshCopy.Runtime
{
    /// <summary>
    /// Author: George Adamopoulos
    /// Version: 2.0.0
    /// Date: 2021-06-15
    /// License: GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
    /// </summary>
    public static class HelperJobs
    {
        /// <summary>
        /// Transform an initial collection of Np points with Nm matrices, generating a total of Np x Nm points.
        /// </summary>
        [BurstCompile]
        public struct TransformVerticesJob : IJobParallelFor
        {
            /// <summary>
            /// Vertices of original mesh
            /// </summary>
            [ReadOnly]
            public NativeArray<float3> inputVertices;

            /// <summary>
            /// Transformation matrices
            /// </summary>
            [ReadOnly]
            public NativeArray<float4x4> matrices;

            /// <summary>
            /// Big array of all the transformed vertices
            /// </summary>
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> outputVertices;

            public void Execute(int index)
            {
                var vertex = inputVertices[index % inputVertices.Length];

                outputVertices[index] = math.mul(matrices[index / inputVertices.Length], new float4(vertex.xyz, 1)).xyz;
            }
        }
        
        
        /// <summary>
        /// Replicate a collection of indices, while incrementing each batch 
        /// </summary>
        [BurstCompile]
        public unsafe struct OffsetReplicateIndicesJob<T> : IJobParallelFor where T:unmanaged
        {
            [NativeDisableUnsafePtrRestriction]
            public void* inputIndices;

            public int originalIndexCount;
        
            public int originalVertexCount;
        
            [WriteOnly]
            public NativeArray<int> outputIndices;
        
            public void Execute(int index)
            {
                var offset        = (index / originalIndexCount) * originalVertexCount;
                var originalIndex = index                        % originalIndexCount;
          
                var inputIndex = index;
            
                switch (sizeof(T))
                {
                    case 4:
                        inputIndex = (int) *((uint*) inputIndices + originalIndex) + offset;
                        break;
                    case 2:
                        inputIndex = *((ushort*) inputIndices + originalIndex) + offset;
                        break;
                }

                outputIndices[index] = inputIndex;
            }
        }

    }
}