using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace NavigationGraph.Graph
{
    [BurstCompile]
    internal struct PrepareRaycastCommandsJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<RaycastCommand> commands;
        public Vector3 origin;
        public int ignoreMasks;
        public int gridSizeX;
        public int gridSizeY;
        public int walkableMask;
        public float cellDiameter;
        public PhysicsScene physicsScene;

        public void Execute(int i)
        {
            int x = i % gridSizeX;
            int y = i / gridSizeX;

            Vector3 cellPosition = origin
                + Vector3.right * ((x + 0.5f) * cellDiameter)
                + Vector3.forward * ((y + 0.5f) * cellDiameter);

            var queryParams = new QueryParameters { layerMask = ~ignoreMasks };

            commands[i] = new RaycastCommand(physicsScene, cellPosition + Vector3.up * gridSizeY, Vector3.down, queryParams, gridSizeY);
        }
    }
}
