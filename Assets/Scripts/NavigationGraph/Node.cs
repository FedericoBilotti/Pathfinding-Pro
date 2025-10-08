using System;
using Unity.Mathematics;

namespace NavigationGraph
{
    public struct Node : IEquatable<Node>
    {
        public float3 position;
        public float3 normal;
        public float height;
        public int gridIndex;
        public int gridX;
        public int gridZ;
        public int cellCostPenalty;
        public WalkableType walkableType;

        public readonly bool Equals(Node other) => gridX == other.gridX && gridZ == other.gridZ;
        public override readonly int GetHashCode() => (int)math.hash(new int3(gridX, gridZ, gridIndex));
    }

    public struct PathNodeData : IHeapComparable<PathNodeData>
    {
        public int cellIndex;
        public int cameFrom;
        public int gCost;
        public int hCost;
        public int FCost => gCost + hCost;

        public int HeapIndex { get; set; }

        public int CompareTo(PathNodeData other)
        {
            int result = FCost.CompareTo(other.FCost);
            if (result == 0) result = hCost.CompareTo(other.hCost);
            return result;
        }
    }
}