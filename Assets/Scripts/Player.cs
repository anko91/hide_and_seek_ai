using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]
    private Rigidbody _rb;

    [SerializeField]
    private float _playerSpeed;

    public void Move(Vector3 moveVector)
    {
        _rb.velocity = moveVector.normalized * _playerSpeed;
    }
}
