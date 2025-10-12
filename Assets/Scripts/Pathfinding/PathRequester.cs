using Agents;
using NavigationGraph;
using UnityEngine;
using Utilities;

namespace Pathfinding
{
    [DefaultExecutionOrder(-800)]
    public class PathRequester : MonoBehaviour, IPathRequest
    {
        [SerializeField] private PathRequestType _requestType;
        private IPathfinding _pathRequestStrategy;

        private void Start() => SetPathStrategy(_requestType);

        public void SetPathStrategy(PathRequestType _requestType)
        {
            var navigationGraph = ServiceLocator.Instance.GetService<INavigationGraph>();
#if UNITY_EDITOR
            if (navigationGraph == null)
            {
                throw new System.Exception("No Navigation Graph found in the scene, please add one");
            }
#endif
            _pathRequestStrategy?.Clear();
            _pathRequestStrategy = PathFactory.CreatePathRequester(_requestType, navigationGraph);
        }

        public bool RequestPath(IAgent agent, Node start, Node end) => _pathRequestStrategy.RequestPath(agent, start, end);
        private void LateUpdate() => _pathRequestStrategy.SetPathToAgent();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (ServiceLocator.Instance == null) return;
            if (ServiceLocator.Instance.GetService<INavigationGraph>() == null) return;
            if (Application.isPlaying && enabled)
                SetPathStrategy(_requestType);
        }
#endif

        private void OnDestroy() => _pathRequestStrategy?.Clear();
    }

    public enum PathRequestType
    {
        AStar,
        ThetaStar
    }
}