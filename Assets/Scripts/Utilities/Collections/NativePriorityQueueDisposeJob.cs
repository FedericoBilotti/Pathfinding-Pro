using System;
using UnityEngine;
using Unity.Jobs;

namespace Utilities.Collections
{
    internal struct NativePriorityQueueDisposeJob<T> : IJob where T : unmanaged, IComparable<T>
    {
        internal NativePriorityQueueDispose<T> Data;

        public void Execute() => Data.Dispose();
    }
}