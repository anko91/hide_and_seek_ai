using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField]
    private NavigationAgentsCommander _commander;
    public NavigationAgentsCommander Commander => _commander;

    private static Game _instance;
    public static Game Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }
}
