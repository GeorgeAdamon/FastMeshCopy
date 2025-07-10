using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityUnsafeUtility = Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

using Unity.Jobs;

namespace UnchartedLimbo.Tools.FastMeshCopy.Runtime
{
    /// <summary>
    /// Author: George Adamopoulos
    /// Version: 2.0.0
    /// Date: 2021-06-15
    /// License: GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
    /// </summary>
    public static class UnsafeUtility
    {
        /// <summary>
        /// Parallel version of <see cref="Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpyReplicate"/>
        /// </summary>
        public static unsafe void MemCpyReplicate<T>(NativeArray<T> destination, NativeArray<T> source, int count) where T:unmanaged
        {
            if (destination.Length < source.Length * count)
            {
                throw new
                        IndexOutOfRangeException($"The destination array cannot fit more than {destination.Length / source.Length} copies of the source array." +
                                                 $"You attempted to shove {count} copies, and this would lead to an immediate and fatal crash. Aborted.");
            }
            new MemCpyReplicateJob<T>
            {
                    sourcePtr    = (T*) source.GetUnsafeReadOnlyPtr(),
                    destPtr      = (T*) destination.GetUnsafePtr(),
                    sourceLength = source.Length
                       
            }.Schedule(count, 64).Complete();
        }

        [BurstCompile]
        private unsafe struct MemCpyReplicateJob <T>: IJobParallelFor where T:unmanaged
        {
            [NativeDisableUnsafePtrRestriction]
            public T* sourcePtr;
           
            [NativeDisableUnsafePtrRestriction]
            public T* destPtr;
            
            public int sourceLength;
            
            public void Execute(int index)
            { 
                UnityUnsafeUtility.MemCpy(destPtr + index * sourceLength, sourcePtr, sourceLength * UnityUnsafeUtility.SizeOf<T>());
            }
        }
    }
}