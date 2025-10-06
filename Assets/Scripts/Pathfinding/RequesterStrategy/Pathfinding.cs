using System;
using Agents;
using NavigationGraph;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Pool;
using Utilities;
using Utilities.Collections;

namespace Pathfinding.RequesterStrategy
{
    public abstract class Pathfinding : IPathRequest, IDisposable
    {
        protected readonly INavigationGraph navigationGraph;

        protected SwapBackListIndexed<PathRequest> requests;
        protected ObjectPool<PathRequest> pathRequestPool;

        protected Pathfinding(INavigationGraph navigationGraph)
        {
            this.navigationGraph = navigationGraph;
            InitializeRequesters();

            navigationGraph.OnCreateGrid += FinishAllPaths;
        }

        private void InitializeRequesters()
        {
            const int CAPACITY = 20;
            const int MAX_SIZE = 1000;
            requests = new SwapBackListIndexed<PathRequest>(CAPACITY);
            pathRequestPool = new ObjectPool<PathRequest>(createFunc: () => new PathRequest
            {
                path = new NativeList<Node>(30, Allocator.Persistent),
                simplified = new NativeList<Node>(30, Allocator.Persistent),
                closedList = new NativeHashSet<int>(64, Allocator.Persistent),
                openList = new NativePriorityQueue<PathNodeData>(navigationGraph.GetGridSizeLength() / 4, Allocator.Persistent),
                visitedNodes = new NativeHashMap<int, PathNodeData>(64, Allocator.Persistent),
                Index = -1
            }, actionOnRelease: pathReq =>
            {
                pathReq.path.Clear();
                pathReq.simplified.Clear();
                pathReq.closedList.Clear();
                pathReq.openList.Clear();
                pathReq.visitedNodes.Clear();
                pathReq.agent = null;
                pathReq.Index = -1;
            }, actionOnDestroy: pathReq =>
            {
                if (pathReq.path.IsCreated) pathReq.path.Dispose(pathReq.handle);
                if (pathReq.simplified.IsCreated) pathReq.simplified.Dispose(pathReq.handle);
                if (pathReq.closedList.IsCreated) pathReq.closedList.Dispose(pathReq.handle);
                if (pathReq.openList.IsCreated) pathReq.openList.Dispose(pathReq.handle);
                if (pathReq.visitedNodes.IsCreated) pathReq.visitedNodes.Dispose(pathReq.handle);
            },
            defaultCapacity: CAPACITY,
            maxSize: MAX_SIZE);
        }

        public abstract bool RequestPath(IAgent agent, Node start, Node end);

        public virtual void SetPathToAgent()
        {
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                var req = requests[i];
                if (!req.handle.IsCompleted) continue;

                req.handle.Complete();
                req.agent.SetPath(ref req.path);

                requests.RemoveAtSwapBack(req);
                pathRequestPool.Release(req);
            }
        }

        private void FinishAllPaths()
        {
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                var req = requests[i];
                req.handle.Complete();
                req.agent.SetPath(ref req.path);

                requests.RemoveAtSwapBack(req);
                pathRequestPool.Release(req);
            }
        }

        public void Dispose()
        {
            foreach (var pathReq in requests)
            {
                DisposePathRequest(pathReq);
            }

            requests.Clear();
            pathRequestPool.Dispose();
            navigationGraph.OnCreateGrid -= FinishAllPaths;
        }

        private void DisposePathRequest(PathRequest pathReq)
        {
            if (pathReq.path.IsCreated) pathReq.path.Dispose(pathReq.handle);
            if (pathReq.simplified.IsCreated) pathReq.simplified.Dispose(pathReq.handle);
            if (pathReq.closedList.IsCreated) pathReq.closedList.Dispose(pathReq.handle);
            if (pathReq.openList.IsCreated) pathReq.openList.Dispose(pathReq.handle);
            if (pathReq.visitedNodes.IsCreated) pathReq.visitedNodes.Dispose(pathReq.handle);
        }

        public void Clear() => Dispose();

        protected class PathRequest : IIndexed
        {
            public IAgent agent;
            public JobHandle handle;

            public NativeList<Node> path;
            public NativeList<Node> simplified;
            public NativeHashSet<int> closedList;
            public NativePriorityQueue<PathNodeData> openList;
            public NativeHashMap<int, PathNodeData> visitedNodes;

            public int Index { get; set; }
        }
    }
}