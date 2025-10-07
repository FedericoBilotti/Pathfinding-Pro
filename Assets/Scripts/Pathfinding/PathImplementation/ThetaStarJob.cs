using NavigationGraph;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Pathfinding.PathImplementation
{
    [BurstCompile]
    internal struct ThetaStarJob : IJob
    {
        [ReadOnly] public NativeArray<Node> grid;
        [ReadOnly] public int gridSizeX;

        public NativeList<Node> finalPath;
        public NativeList<Node> simplified;

        public void Execute()
        {
            SimplifyPath();
            ReversePath();
        }

        private void ReversePath()
        {
            finalPath.Clear();
            finalPath.AddRange(simplified.AsArray());
        }

        private void SimplifyPath()
        {
            // Avoid simplifying the path if it has less than 2 cells
            if (finalPath.Length <= 2)
                return;

            int j = 0;
            simplified.Add(finalPath[0]);

            for (int i = 1; i < finalPath.Length; i++)
            {
                if (HasLineOfSight(finalPath[j], finalPath[i])) continue;

                simplified.Add(finalPath[i - 1]);
                j = i - 1;
            }
        }

        // Bresenham algorithm
        private bool HasLineOfSight(in Node startCell, in Node endCell)
        {
            int startX = startCell.gridX;
            int startY = startCell.gridZ;
            int endX = endCell.gridX;
            int endY = endCell.gridZ;

            int deltaX = math.abs(endX - startX);
            int deltaY = math.abs(endY - startY);

            int stepX = startX < endX ? 1 : -1;
            int stepY = startY < endY ? 1 : -1;

            int error = deltaX - deltaY;

            while (startX != endX || startY != endY)
            {
                int index = startX + startY * gridSizeX;

                if (grid[index].walkableType != WalkableType.Walkable) return false;

                // For now this work, but maybe when Links are implemented it should be changed.
                if (math.abs(grid[index].position.y - startCell.position.y) > 0.0001f) return false;
                if (math.abs(grid[index].position.y - endCell.position.y) > 0.0001f) return false;

                int doubleError = 2 * error;

                if (doubleError > -deltaY)
                {
                    error -= deltaY;
                    startX += stepX;
                }

                if (doubleError < deltaX)
                {
                    error += deltaX;
                    startY += stepY;
                }
            }

            return true;
        }
    }
}