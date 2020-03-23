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

    [SerializeField] 
    [Range(0, 10)]
    private int _navigationStepWeight = 3;

    public int NavigationStepWeight { get; set; }
    
    
    private Queue<NavigationPoint> _queue = new Queue<NavigationPoint>();
    
    private const int VISIBLE_BY_PLAYER = 100000;
    private const int UNPASSABLE = 1000000;

    public delegate int AdditionalCostDelegate(NavigationAgent agent, Vector3 point);

    public AdditionalCostDelegate additionalCost;

    public void FixedUpdate()
    {
        if (Vector3.Distance(_cachedPlayerPosition, _player.position) > 1f)
        {
            UpdateWeights();
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
        UpdateWeights();
    }

    private NavigationPoint GetNearestNavigationPoint(Vector3 point)
    {
        var minValue = float.MaxValue;
        var minIndex = 0;
        for(var i = 0; i < _navPoints.Length; i++)
        {
            var dist = Vector3.Distance(_navPoints[i].position, point);
            if (dist < minValue)
            {
                minValue = dist;
                minIndex = i;
            }
        }
        return _navPoints[minIndex];
    }


    private void UpdateWeights()
    {
        //System.Diagnostics.Stopwatch SW = new System.Diagnostics.Stopwatch(); 
        //SW.Start(); 
        
        var beginPoint = GetNearestNavigationPoint(_player.transform.position);
        _queue.Clear();
        _queue.Enqueue(beginPoint);
        beginPoint.weight = 0;
        for (var i = 0; i < _navPoints.Length; i++)
        {
            _navPoints[i].weight = 0;
            _navPoints[i].visited = false;
        }

        beginPoint.weight = 0;

        while (_queue.Count > 0)
        {
            var navPoint = _queue.Dequeue();
            navPoint.visited = true;
            
            for (var i = -1; i <= 1; i++)
            {
                for (var j = -1; j <= 1; j++)
                {
                    var index = navPoint.index + i * height + j;
                    if (index >= 0 && index < _navPoints.Length)
                    {
                        if (!_navPoints[index].visited && _navPoints[index].isPassable)
                        {
                            _navPoints[index].weight = navPoint.weight + _navigationStepWeight;
                            _navPoints[index].visited = true;
                            _queue.Enqueue(_navPoints[index]);
                        }
                    }
                }
            }
            
        }

        for (var i = 0; i < width; i++)
        {
            for (var j = 0; j < height; j++)
            {
                if (_navPoints[i * height + j].isPassable)
                {
                    if (PointIsVisibleFromPlayerPosition(_navPoints[i * height + j]))
                    {
                        _navPoints[i * height + j].weight += VISIBLE_BY_PLAYER;
                    }
                }
            }
        }    
       // SW.Stop();
        //Debug.Log(SW.ElapsedMilliseconds);
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
        var minCost = int.MaxValue;
        var minIndex = 0;
            
        for (var i = 0; i < _navPoints.Length; i++)
        {
            var cost = ComputeCost(_navPoints[i], agent);
            if (cost < minCost)
            {
                minCost = cost;
                minIndex = i;
            }
        }
        
        return _navPoints[minIndex].position;
    }

    private int ComputeCost(NavigationPoint navPoint, NavigationAgent agent)
    {
        return navPoint.weight
               + (additionalCost == null ? 0 : additionalCost.Invoke(agent, navPoint.position))
               + GetNeighboursCost(navPoint)
               + (navPoint.isPassable ? 0 : UNPASSABLE);
    }
    
    private int GetNeighboursCost(NavigationPoint point)
    {
        var cost = 0;
        for (var i = -1; i <= 1; i++)
        {
            for (var j = -1; j <= 1; j++)
            {
                var index = point.index + i * height + j;
                if (index != point.index && index >= 0 && index < _navPoints.Length)
                {
                    cost += Mathf.RoundToInt(_navPoints[index].weight * 0.1f);
                }
            }
        }
        return cost;
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
        public int weight;

        public int index;
        public bool visited;
    }
}
