using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnemyController : GazePointer
{
    public GameObject particleEffect;
    public Animator enemyModel;
    Vector3 endPos;
    public float speed, hitPower, priceScore;
    private float enemyHealth;
    public Image healthImg;

    BulletSpawner bulletSpawner;
    StromBreaker axe;
    // Start is called before the first frame update
    void Start()
    {
        endPos = 1.5f * (transform.position - Vector3.zero).normalized;
        bulletSpawner = GameObject.FindObjectOfType<BulletSpawner>();
        axe = GameObject.FindObjectOfType<StromBreaker>();
        enemyHealth = 1f;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position, endPos, speed * Time.deltaTime);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        // bulletSpawner.ShootBullet();
        axe.Throw();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Attack();
        }
        else if (other.CompareTag("Enemy"))
        {
            WithOutScoreDeath();
            // Destroy(gameObject);
        }
    }

    public void Death()
    {
        // enemyModel.SetTrigger("death");
        enemyHealth -= hitPower;
        healthImg.fillAmount = enemyHealth;
        Debug.Log(enemyHealth);
        if (enemyHealth <= 0)
        {
            particleEffect.SetActive(true);
            particleEffect.transform.SetParent(null);
            Destroy(gameObject);
            PlayerManager.currentScore += priceScore;
        }
    }

    public void WithOutScoreDeath()
    {
        particleEffect.SetActive(true);
        particleEffect.transform.SetParent(null);
        Destroy(gameObject);
    }

    public void Attack()
    {
        enemyModel.SetTrigger("attack");
        PlayerManager.playerHealth -= 0.1f;
    }
}
