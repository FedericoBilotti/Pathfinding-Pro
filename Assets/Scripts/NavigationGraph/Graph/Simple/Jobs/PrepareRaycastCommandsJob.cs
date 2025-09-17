using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace NavigationGraph.Graph
{
    internal sealed partial class SimpleGridNavigationGraph
    {
        #region Jobs & Burst

        [BurstCompile]
        private struct PrepareRaycastCommandsJob : IJobParallelFor
        {
            [WriteOnly] public NativeArray<RaycastCommand> commands;
            public Vector3 origin;
            public float cellDiameter;
            public int gridSizeX;
            public int gridSizeY;
            public int walkableMask;
            public PhysicsScene physicsScene;

            public void Execute(int i)
            {
                int x = i % gridSizeX;
                int y = i / gridSizeX;

                Vector3 cellPosition = origin
                    + Vector3.right * ((x + 0.5f) * cellDiameter)
                    + Vector3.forward * ((y + 0.5f) * cellDiameter);
                
                var queryParams = new QueryParameters { layerMask = ~0 };

                commands[i] = new RaycastCommand(physicsScene, cellPosition + Vector3.up * gridSizeY, Vector3.down, queryParams, gridSizeY);
            }
        }
    }

    #endregion
}
