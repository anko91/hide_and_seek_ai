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
    [Range(1, 100f)]
    private float _playerLookRadius = 5;

    public int NavigationStepWeight { get; set; }
    
    
    private Queue<NavigationPoint> _queue = new Queue<NavigationPoint>();

    private const int VISIBLE_BY_PLAYER = 10000;
    private const int NEIGHTBOUR_OF_VISIBLE_BY_PLAYER = 10;
    private const int UNPASSABLE = 1000000;

    public delegate float AdditionalCostDelegate(NavigationAgent agent, Vector3 point);

    public AdditionalCostDelegate additionalCost;

    [SerializeField]
    [Range(0, 50)]
    private int _computePlayerDistanceLimit = 25;

    [SerializeField]
    [Range(0, 50)]
    private int _computeAgentDistanceLimit = 15;

    public void Update()
    {
        if (Vector3.Distance(_cachedPlayerPosition, _player.position) > 0.1f)
        {
            UpdateDistanceToPlayer();
            _cachedPlayerPosition = _player.position;
        }
    }

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
        UpdateDistanceToPlayer();
    }

    private NavigationPoint GetNearestNavigationPoint(Vector3 point)
    {
        var minValue = float.MaxValue;
        var minIndex = 0;
        for(var i = 0; i < _navPoints.Length; i++)
        {
            if (_navPoints[i].isPassable)
            {
                var dist = Vector3.Distance(_navPoints[i].position, point);
                if (dist < minValue)
                {
                    minValue = dist;
                    minIndex = i;
                }
            }
        }
        return _navPoints[minIndex];
    }


    private void UpdateDistanceToPlayer()
    {        
        var beginPoint = GetNearestNavigationPoint(_player.transform.position);
        _queue.Clear();
        _queue.Enqueue(beginPoint);

        for (var i = 0; i < _navPoints.Length; i++)
        {
            _navPoints[i].distanceToPlayerWeight = _computePlayerDistanceLimit;
            _navPoints[i].visited = false;
            _navPoints[i].deepness = 0;
        }

        beginPoint.distanceToPlayerWeight = 0;

        while (_queue.Count > 0)
        {
            var navPoint = _queue.Dequeue();
            navPoint.visited = true;
            
            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    var index = navPoint.index + i * height + j;
                    if (index >= 0 && index < _navPoints.Length && navPoint.index % height + j >= 0 && navPoint.index % height + j < height)
                    {
                        if (!_navPoints[index].visited && navPoint.deepness < _computePlayerDistanceLimit && _navPoints[index].isPassable)
                        {
                            _navPoints[index].distanceToPlayerWeight = navPoint.distanceToPlayerWeight + 1;
                            _navPoints[index].visited = true;
                            _navPoints[index].deepness = navPoint.deepness + 1;
                            _queue.Enqueue(_navPoints[index]);
                        }
                    }
                }
            }            
        }
    }

    private bool PointIsVisibleFromPlayerPosition(NavigationPoint navPoint)
    {
        var origin = navPoint.position + Vector3.up;
        var playerPos = _player.transform.position;
        RaycastHit hit;
        var distanceFromPlayerToPoint = Vector3.Magnitude(playerPos - origin);
        int layerMask = _player.gameObject.layer;
        return !(distanceFromPlayerToPoint > _playerLookRadius || Physics.Raycast(origin, playerPos - origin, out hit, distanceFromPlayerToPoint, layerMask));
    }

    public Vector3 FindNearestSafePoint(NavigationAgent agent)
    {
        var minCost = float.MaxValue;
        var minIndex = -1;
        var maxCost = float.MinValue;
        var maxIndex = -1;
        

        var beginPoint = GetNearestNavigationPoint(agent.transform.position);

        _queue.Clear();
        _queue.Enqueue(beginPoint);

        for (var i = 0; i < _navPoints.Length; i++)
        {
            _navPoints[i].distanceToAgentWeight = _computeAgentDistanceLimit;
            _navPoints[i].visited = false;
            _navPoints[i].deepness = 0;
        }
        beginPoint.distanceToAgentWeight = 0;
        beginPoint.visited = true;

        while (_queue.Count > 0)
        {
            var navPoint = _queue.Dequeue();
            var cost = ComputeCost(navPoint, agent);
            navPoint.cost = cost;
            if (cost + GetNeightboursCost(navPoint) < minCost)
            {
                minCost = cost + GetNeightboursCost(navPoint);
                minIndex = navPoint.index;
            }
            if (cost > maxCost)
            {
                maxCost = cost;
                maxIndex = navPoint.index;
            }

            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    var index = navPoint.index + i * height + j;
                    if (index >= 0 && index < _navPoints.Length && navPoint.index % height + j >= 0 && navPoint.index % height + j < height)
                    {
                        if (!_navPoints[index].visited && navPoint.deepness < _computeAgentDistanceLimit && _navPoints[index].isPassable)
                        {
                            _navPoints[index].distanceToAgentWeight = navPoint.distanceToAgentWeight + 1;
                            _navPoints[index].visited = true;
                            _navPoints[index].deepness = navPoint.deepness + 1;
                            _queue.Enqueue(_navPoints[index]);
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
                    if (_navPoints[index].isPassable && _navPoints[index].cost >= VISIBLE_BY_PLAYER)
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
        var baseDistance = navPoint.distanceToPlayerWeight * 2
                + navPoint.distanceToAgentWeight;
        if (navPoint.distanceToPlayerWeight >= _computePlayerDistanceLimit)
        {
            baseDistance = _computePlayerDistanceLimit + Vector3.Distance(navPoint.position, _player.position);
        }
            return baseDistance
                + (additionalCost == null ? 0 : additionalCost.Invoke(agent, navPoint.position))
                   + (navPoint.isPassable ? 0 : UNPASSABLE)
                   + (PointIsVisibleFromPlayerPosition(navPoint) ? VISIBLE_BY_PLAYER : 0);
        
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
        [FormerlySerializedAs("pos")] public Vector3 position;
        public bool isPassable;
        public float distanceToPlayerWeight;
        public float distanceToAgentWeight;
        public float cost;


        public int index;
        public bool visited;
        public int deepness;
    }
}
