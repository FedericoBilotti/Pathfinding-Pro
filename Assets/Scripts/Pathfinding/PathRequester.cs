using Agents;
using NavigationGraph;
using UnityEngine;

namespace Pathfinding
{
    [DefaultExecutionOrder(-800)]
    public class PathRequester : MonoBehaviour, IPathfinding
    {
        [SerializeField] private PathRequestType _requestType;
        private IPathRequest _pathRequestStrategy;

        private void Awake() => ServiceLocator.Instance.RegisterService<IPathfinding>(this);

        void Start() => SetPathStrategy(_requestType);

        public void SetPathStrategy(PathRequestType _requestType)
        {
            var navigationGraph = ServiceLocator.Instance.GetService<INavigationGraph>();
            _pathRequestStrategy = PathFactory.CreatePathRequester(_requestType, navigationGraph);
            Debug.Log("Path Requester Strategy set to: " + _requestType);
        }

        public bool RequestPath(IAgent agent, Cell start, Cell end)
        {
            Debug.Log("Requesting path");
            bool p = _pathRequestStrategy.RequestPath(agent, start, end);

            Debug.Log(p);
            return p;
        }
        private void LateUpdate()
        {
            Debug.Log("Setting path to agent");
            _pathRequestStrategy.SetPathToAgent();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                SetPathStrategy(_requestType);
        }
#endif

        private void OnDestroy() => _pathRequestStrategy.Clear();
    }

    public enum PathRequestType
    {
        AStar,
        ThetaStar
    }
}