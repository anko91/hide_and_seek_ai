using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class NavigationAgentsCommander : MonoBehaviour
{

    [SerializeField]
    [Range(1f, 100f)]
    private float _agentsPositionDistance = 5f;

    [SerializeField]
    [Range(0f, 5f)]
    private float _agentsMovementCost = 1f;

     [SerializeField]
    private NavigationArea _navigationArea;

    private List<NavigationAgent> _agents = new List<NavigationAgent>();

    public void RegisterAgent(NavigationAgent agent)
    {
        _agents.Add(agent);
    }

    public void UnregisterAgent(NavigationAgent agent)
    {
        _agents.Remove(agent);
    }

    private void Start()
    {
        StartCoroutine(UpdateNavigationPointsRoutine());
        _navigationArea.additionalCost = AdditionalCost;
    }

    public IEnumerator UpdateNavigationPointsRoutine()
    {
        while (true)
        {
            for (var i = 0; i < _agents.Count; i++)
            {
                var point = _navigationArea.FindNearestSafePoint(_agents[i]);
                _agents[i].MoveTo(point);
                yield return null;
            }
            yield return null;
        }
    }

    public int AdditionalCost(NavigationAgent currentAgent, Vector3 point)
    {
        var cost = 0;
        for (var i = 0; i < _agents.Count; i++)
        {
            if (_agents[i] != currentAgent)
            {
                cost += Mathf.RoundToInt(_agentsPositionDistance / (Vector3.Distance(_agents[i].TargetPoint, point) + 1));
            }
        }
        cost += Mathf.RoundToInt(Vector3.Distance(currentAgent.transform.position, point) * _agentsMovementCost);
        return cost;
    }
    
    private void OnDrawGizmos()
    {
        foreach (var agent in _agents)
        {
            var point = agent.TargetPoint;
            var red = Color.red;
            red.a = 0.5f;
            Gizmos.color = red;
            Gizmos.DrawSphere(point, 2f);
        }
    }
}
