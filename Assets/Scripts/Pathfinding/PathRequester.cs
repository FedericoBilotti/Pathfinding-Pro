using Agents;
using NavigationGraph;
using Pathfinding.RequesterStrategy;
using UnityEngine;

namespace Pathfinding
{
    [DefaultExecutionOrder(-800)]
    public class PathRequester : MonoBehaviour, IPathfinding
    {
        [SerializeField] private PathRequestType _requestType;
        private IPathRequest _aStar;
        private IPathRequest _thetaStar;

        private void Awake() => ServiceLocator.Instance.RegisterService<IPathfinding>(this);

        void Start()
        {
            INavigationGraph navigationGraph = ServiceLocator.Instance.GetService<INavigationGraph>();
            _aStar = new AStarRequester(navigationGraph);
            _thetaStar = new ThetaStarRequester(navigationGraph);
        }

        public bool RequestPath(IAgent agent, Cell start, Cell end)
        {
            return _requestType switch
            {
                PathRequestType.AStar => _aStar.RequestPath(agent, start, end),
                PathRequestType.ThetaStar => _thetaStar.RequestPath(agent, start, end),
                _ => throw new System.Exception("Unknown request type")
            };
        }

        private void LateUpdate()
        {
            switch (_requestType)
            {
                case PathRequestType.AStar:
                    _aStar.FinishPath();
                    break;

                case PathRequestType.ThetaStar:
                    _thetaStar.FinishPath();
                    break;

                default:
                    throw new System.Exception("Unknown request type");
            }
        }

        private enum PathRequestType
        {
            None,
            AStar,
            ThetaStar
        }

        private void OnDestroy()
        {
            _thetaStar.Clear();
            _aStar.Clear();
        }
    }
}