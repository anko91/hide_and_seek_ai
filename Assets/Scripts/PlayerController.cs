using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Player _player;

    private void Update()
    {
        var moveVector = Vector3.zero;
        if (LeftPressed())
        {
            moveVector.x -= 1;
        }
        if (RightPressed())
        {
            moveVector.x += 1;
        }
        if (UpPressed())
        {
            moveVector.z += 1;
        }
        if (DownPressed())
        {
            moveVector.z -= 1;
        }
        _player.Move(moveVector);
    }

    private bool LeftPressed()
    {
        return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
    }

    private bool RightPressed()
    {
        return Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
    }

    private bool UpPressed()
    {
        return Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
    }

    private bool DownPressed()
    {
        return Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
    }
}
