using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    public GameObject bullet;
    public float speed = 1f;

    public void ShootBullet()
    {
        var bulletInstance = Instantiate(bullet);
        bulletInstance.transform.position = transform.position;
        bullet.GetComponent<Rigidbody>().velocity = transform.forward * speed;
    }
}
