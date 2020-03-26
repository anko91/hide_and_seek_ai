using UnityEngine;
using UnityEngine.AI;

public class NavigationAgent : MonoBehaviour
{
    [SerializeField]
    private NavigationArea _navigationArea;

    [SerializeField]
    private NavMeshAgent _agent;

    private Vector3 _targetPoint;
    public Vector3 TargetPoint => _targetPoint;

    private NavMeshPath _path;
    public NavMeshPath LastComputedPath => _path;
    public int Id { get; private set; }
    private static int IdCounter;

    private void Start()
    {
        Id = IdCounter++;
        _path = new NavMeshPath();
        Game.Instance.Commander.RegisterAgent(this);
        _targetPoint = transform.position;
    }

    private void OnDestroy()
    {
        Game.Instance.Commander.UnregisterAgent(this);
    }

    public void MoveTo(Vector3 targetPoint)
    {
        _targetPoint = targetPoint;
        _agent.destination = targetPoint;
    }

    private Vector3 _cachedPosition = Vector3.positiveInfinity;

    public Vector3 GetNearestPointToTarget(Vector3 target)
    {
        if (Vector3.Distance(_cachedPosition, transform.position) < 1.0f && Vector3.Distance(_path.corners[_path.corners.Length - 1], target) < 1.0f)
        {
            return _path.corners[_i];
        }
        _path.ClearCorners();
        _agent.CalculatePath(target, _path);
        for (var i = 0; i < _path.corners.Length; i++)
        {
            if (Vector3.Distance(transform.position, _path.corners[i]) > 2f)
            {
                _i = i;
                return _path.corners[i];
            }
        }
        _i = 0;
        return transform.position;
    }

    private int _i;
    private void OnDrawGizmos()
    {
        var white = Color.white;
        white.a = 0.5f;
        Gizmos.color = white;
        if (_path != null)
        {
            for (var i = 0; i < _path.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(_path.corners[i], _path.corners[i + 1]);
                Gizmos.DrawSphere(_path.corners[i], 0.5f);
            }
            if (_path.corners.Length > 1)
            {
                Gizmos.DrawSphere(_path.corners[_i], 1.5f);
            }
        }
        var red = Color.red;
        red.a = 0.5f;
        Gizmos.color = red;
        Gizmos.DrawSphere(_targetPoint, 2f);

        Gizmos.color = Color.black;
        var lookDist = _navigationArea.ComputeAgentDistanceLimit;
        Gizmos.DrawLine(transform.position, _targetPoint);

        Gizmos.DrawLine(transform.position + new Vector3(lookDist, 0, lookDist), transform.position + new Vector3(lookDist, 0, -lookDist));
        Gizmos.DrawLine(transform.position + new Vector3(lookDist, 0, -lookDist), transform.position + new Vector3(-lookDist, 0, -lookDist));
        Gizmos.DrawLine(transform.position + new Vector3(-lookDist, 0, -lookDist), transform.position + new Vector3(-lookDist, 0, lookDist));
        Gizmos.DrawLine(transform.position + new Vector3(-lookDist, 0, lookDist), transform.position + new Vector3(lookDist, 0, lookDist));
    }
}
