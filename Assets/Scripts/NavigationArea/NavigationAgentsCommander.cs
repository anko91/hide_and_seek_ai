using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class NavigationAgentsCommander : MonoBehaviour
{
     [SerializeField]
    private NavigationArea _navigationArea;

    private List<NavigationAgent> _agents = new List<NavigationAgent>();
    [SerializeField]

    [Range(0f, 10f)]
    private float _minDistanceBetweenAgents = 3f;

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
            //yield return null;
        }
    }
    
    public float AdditionalCost(NavigationAgent currentAgent, Vector3 point)
    {
        var cost = 0f;
        for (var i = 0; i < _agents.Count; i++)
        {
            if (_agents[i] != currentAgent)
            {
                var dist = Vector3.Distance(_agents[i].TargetPoint, point);
                if (dist < _minDistanceBetweenAgents)
                {
                    cost += 1000f;
                }
            }
        }
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

            Gizmos.color = Color.black;
            Gizmos.DrawLine(agent.transform.position, agent.TargetPoint);
        }
    }
}
