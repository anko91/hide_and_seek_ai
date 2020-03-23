using UnityEngine;
using UnityEngine.AI;

public class NavigationAgent : MonoBehaviour
{
    [SerializeField]
    private NavigationArea _navigationArea;

    [SerializeField]
    private NavMeshAgent _agent;

    [SerializeField]
    private Vector3 _targetPoint;
    public Vector3 TargetPoint => _targetPoint;

    private void Start()
    {
        Game.Instance.Commander.RegisterAgent(this);
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
}
