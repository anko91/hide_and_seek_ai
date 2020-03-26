using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public partial class NavigationArea : MonoBehaviour
{
    [SerializeField]
    [Range(1, 100)]
    private int _areaWidth = 5;

    [SerializeField]
    [HideInInspector]
    private int width;

    [SerializeField]
    [Range(1, 100)]
    private int _areaHeight = 5;

    [SerializeField]
    [HideInInspector]
    private int height;

    [SerializeField]
    [Range(0.2f, 3f)]
    private float _meshStep = 1;

    [SerializeField]
    [HideInInspector]
    private NavigationPoint[] _navPoints;


    [SerializeField]
    private Transform _player;
    public Transform Player => _player;

    private Vector3 _cachedPlayerPosition = Vector3.positiveInfinity;
    [SerializeField]
    [Range(0, 100f)]
    private float _playerLookRadius = 5f;

    [SerializeField]
    private NavigationType _navigationType = NavigationType.SimpleNearestPoint;

    public int NavigationStepWeight { get; set; }
    
    
    private Queue<NavigationPoint> _queue = new Queue<NavigationPoint>();

    private const int VISIBLE_BY_PLAYER = 10000;
    private const int NEIGHTBOUR_OF_VISIBLE_BY_PLAYER = 10;
    private const int UNPASSABLE = 1000000;

    public delegate float AdditionalCostDelegate(NavigationAgent agent, Vector3 point);

    public AdditionalCostDelegate additionalCost;
    
    [SerializeField]
    [Range(0, 50)]
    private int _computeAgentDistanceLimit = 15;
    public float ComputeAgentDistanceLimit => _computeAgentDistanceLimit * _meshStep;

    [SerializeField]
    private bool _computeNeightboursCost = true;

    public void RebuildArea()
    {
        width = (int)(_areaWidth / _meshStep);
        height = (int)(_areaHeight / _meshStep);
        _navPoints = new NavigationPoint[width * height];
        var pos = transform.position;
        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                var x = -_areaWidth / 2f + i * _meshStep + _meshStep * 0.5f;
                var z = -_areaHeight / 2f + j * _meshStep + _meshStep * 0.5f;
                _navPoints[i * height + j] = new NavigationPoint(pos.x + x, pos.y, pos.z + z, CheckAvailableForPathfinding(x, z));
                _navPoints[i * height + j].index = i * height + j;
            }
        }
    #if UNITY_EDITOR
        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
    #endif
    }

    private NavigationPoint GetNearestNavigationPoint(Vector3 point)
    {
        var i = Mathf.RoundToInt((point.x - transform.position.x + _areaWidth / 2f) / _meshStep);
        var j = Mathf.RoundToInt((point.z - transform.position.z + _areaHeight / 2f) / _meshStep);
        var index = i * height + j;
        if (index < 0 || index >= _navPoints.Length)
        {
            var minValue = float.MaxValue;
            for (var k = 0; k < _navPoints.Length; k++)
            {
                if (_navPoints[k].isPassable)
                {
                    var dist = Vector3.Distance(_navPoints[k].position, point);
                    if (dist < minValue)
                    {
                        minValue = dist;
                        index = k;
                    }
                }
            }
        }
        if (!_navPoints[index].isPassable)
        {
            while (!_navPoints[index].isPassable)
            {
                index = (index + 1) % _navPoints.Length;
            }
        }
        Debug.DrawLine(_navPoints[index].position, _navPoints[index].position + Vector3.up * 10, Color.black);
        return _navPoints[index];
    }

    public Vector3 FindNearestSafePoint(NavigationAgent agent)
    {
        var minCost = float.MaxValue;
        var minIndex = -1;
        var maxCost = float.MinValue;
        var maxIndex = -1;

        var nextPathPoint = Vector3.zero;
        if (_navigationType == NavigationType.ByUnityNavMeshPath)
        {
            agent.GetNearestPointToTarget(_player.position);
        }
        var beginPoint = GetNearestNavigationPoint(agent.transform.position);

        if (agent.TargetPoint != Vector3.negativeInfinity)
        {
            var prevTargetPoint = GetNearestNavigationPoint(agent.TargetPoint);
            minCost = EvaluateCost(prevTargetPoint, agent, nextPathPoint) + ComputeCost(prevTargetPoint, agent);
            minIndex = prevTargetPoint.index;
        }


        for (var i = -_computeAgentDistanceLimit; i <= _computeAgentDistanceLimit; i++)
        {
            for (var j = -_computeAgentDistanceLimit; j<= _computeAgentDistanceLimit; j++)
            {
                var index = beginPoint.index + i * height + j;
                if (index >= 0 && index < _navPoints.Length && beginPoint.index % height + j >= 0 && beginPoint.index % height + j < height && _navPoints[index].isPassable)
                {
                    var cost = EvaluateCost(_navPoints[index], agent, nextPathPoint);
                    if (cost < minCost)
                    {
                        cost += ComputeCost(_navPoints[index], agent);
                        if (cost < minCost)
                        {
                            minCost = cost;
                            minIndex = index;
                        }
                        if (cost > maxCost)
                        {
                            maxCost = cost;
                            maxIndex = index;
                        }
                    }
                }
            }
        }
        if (minCost >= VISIBLE_BY_PLAYER)//if no safe area - run away from player
        {
            return _navPoints[maxIndex].position;
        }

        return _navPoints[minIndex].position;
    }

    private bool PointIsVisibleFromPlayerPosition(NavigationPoint point)
    {
        if (point.lastVisibilityCheckTick == Game.Instance.Commander.NavigationAgentsTick)
        {
            return point.visibleByPlayer;
        }
        var origin = point.position + Vector3.up * 0.5f;
        var playerPos = _player.transform.position;
        RaycastHit hit;
        var target = playerPos - origin;
        var distanceFromPlayerToPoint = Vector3.Magnitude(target);
        int layerMask = _player.gameObject.layer;
        point.lastVisibilityCheckTick = Game.Instance.Commander.NavigationAgentsTick;
        point.visibleByPlayer = !(distanceFromPlayerToPoint > _playerLookRadius || Physics.Raycast(origin, target, out hit, distanceFromPlayerToPoint, layerMask));
        return point.visibleByPlayer;
    }

    private float GetNeightboursCost(NavigationPoint navPoint)
    {
        var cost = 0f;
        for (var i = -1; i <= 1; i++)
        {
            for (var j = -1; j <= 1; j++)
            {
                var index = navPoint.index + i * height + j;
                if (index != navPoint.index && index >= 0 && index < _navPoints.Length && navPoint.index % height + j >= 0 && navPoint.index % height + j < height)
                {                    
                    if (_navPoints[index].isPassable && PointIsVisibleFromPlayerPosition(_navPoints[index]))
                    {
                        cost += NEIGHTBOUR_OF_VISIBLE_BY_PLAYER;
                    }
                }
            }
        }
        return cost;
    }

    private float ComputeCost(NavigationPoint navPoint, NavigationAgent agent)
    {
        var addCost = additionalCost == null ? 0 : additionalCost.Invoke(agent, navPoint.position);
        var neighboursCost = _computeNeightboursCost ? GetNeightboursCost(navPoint) : 0;
        return (PointIsVisibleFromPlayerPosition(navPoint) ? VISIBLE_BY_PLAYER - addCost : addCost) + neighboursCost;
    }

    private float EvaluateCost(NavigationPoint navPoint, NavigationAgent agent, Vector3 nextPathPoint)
    {
        switch(_navigationType)
        {
            case NavigationType.ByUnityNavMeshPath:
                return 2 * Vector3.Distance(navPoint.position, agent.transform.position) + 3f * Vector3.Distance(navPoint.position, nextPathPoint) + 0.5f * Vector3.Distance(navPoint.position, _player.position);
            case NavigationType.SimpleNearestPoint:
            default:
                return 1 * Vector3.Distance(navPoint.position, agent.transform.position) + 2f * Vector3.Distance(navPoint.position, _player.position);
        }
    }

    private bool CheckAvailableForPathfinding(float x, float z)
    {
        var origin = new Vector3(transform.position.x + x, transform.position.y, transform.position.z + z);
        var castRadius = _meshStep * 0.5f;
        var castDistance = 1f;
        var layerMask = _player.gameObject.layer;
        var hits = Physics.SphereCastAll(origin, castRadius, Vector3.up, castDistance, layerMask);
        var availableForPathfinding = true;
        foreach (var hit in hits)
        {
            if (IsObstacle(hit.collider.gameObject))
            {
                availableForPathfinding = false;
            }
        }
        return availableForPathfinding;
    }

    private bool IsObstacle(GameObject go)
    {
        return go != this.gameObject;
    }

    [System.Serializable]
    private class NavigationPoint
    {
        public NavigationPoint(float x, float y, float z, bool passability = true)
        {
            position.x = x;
            position.y = y;
            position.z = z;
            this.isPassable = passability;
        }
        public NavigationPoint(Vector3 position, bool passability = true)
        {
            this.position = position;
            this.isPassable = passability;
        }
        public Vector3 position;
        public bool isPassable;

        public bool visibleByPlayer = false;
        public int lastVisibilityCheckTick = 0;


        public int index;
    }
}

public enum NavigationType
{
    ByUnityNavMeshPath = 1,
    SimpleNearestPoint = 2
}