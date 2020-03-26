using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class NavigationAgentsCommander : MonoBehaviour
{
     [SerializeField]
    private NavigationArea _navigationArea;

    private List<NavigationAgent> _agentsTmp = new List<NavigationAgent>();
    private NavigationAgent[] _agents;

    [SerializeField]
    [Range(0f, 10f)]
    private float _minDistanceBetweenAgents = 5f;

    public int NavigationAgentsTick { get; private set; }

    public void RegisterAgent(NavigationAgent agent)
    {
        _agentsTmp.Add(agent);
        _agents = _agentsTmp.ToArray();
    }

    public void UnregisterAgent(NavigationAgent agent)
    {
        _agentsTmp.Remove(agent);
        _agents = _agentsTmp.ToArray();
    }

    private void Start()
    {
        NavigationAgentsTick = 0;
        StartCoroutine(UpdateNavigationPointsRoutine());
        _navigationArea.additionalCost = AdditionalCost;
    }

    public IEnumerator UpdateNavigationPointsRoutine()
    {
        while (true)
        {
            for (var i = 0; i < _agents.Length; i++)
            {
                var point = _navigationArea.FindNearestSafePoint(_agents[i]);
                _agents[i].MoveTo(point);
                yield return null;
            }
            NavigationAgentsTick++;
        }
    }
    
    public float AdditionalCost(NavigationAgent currentAgent, Vector3 point)
    {
        var cost = 0f;
        for (var i = 0; i < _agents.Length; i++)
        {
            if (_agents[i].Id != currentAgent.Id)
            {
                var dist = Vector3.Distance(_agents[i].TargetPoint, point);
                if (dist < _minDistanceBetweenAgents)
                {
                    cost += 100f * (_minDistanceBetweenAgents - dist) / _minDistanceBetweenAgents;
                }
            }
        }
        return cost;
    }
}
