using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    void Start()
    {
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<EnemyController>())
        {
            other.GetComponent<EnemyController>().Death();
        }
    }
}
