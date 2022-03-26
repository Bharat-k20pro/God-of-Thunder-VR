using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StromBreaker : MonoBehaviour
{
    public Transform hand;
    public Rigidbody rb;
    public bool isHeld, isRetracting;
    public float throwPower, retractPower, rotationSpeed;

    public AudioClip hitAudio, catchAudio;
    AudioSource audiosource;

    // Start is called before the first frame update
    void Start()
    {
        audiosource = gameObject.GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        Catch();
        StartCoroutine(CheckForHeld());
    }

    // Update is called once per frame
    void Update()
    {
        if (!isHeld)
        {
            transform.localEulerAngles += transform.forward * rotationSpeed * Time.deltaTime;
            // audiosource.PlayOneShot(throwAudio);
        }

        // if (!isHeld)
        // {
        //     gameObject.GetComponent<Animator>().Play("Rotate");
        // }
        // else
        // {
        //     gameObject.GetComponent<Animator>().Play("Idle");
        // }
        if (isRetracting && !isHeld)
        {
            Retract();
            // hand.gameObject.SetActive(true);
        }
        // if (!isRetracting && !isHeld)
        // {
        //     hand.gameObject.SetActive(false);
        // }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<EnemyController>())
        {
            // other.SetTrigger("death");
            audiosource.PlayOneShot(hitAudio);
            other.GetComponent<EnemyController>().Death();
            isRetracting = true;
        }
    }

    public void Throw()
    {
        if (GetComponent<Animator>()) GetComponent<Animator>().Play("Rotate");

        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // gameObject.transform.SetParent(null);

        rb.AddForce(transform.forward * throwPower, ForceMode.Impulse);
        isHeld = false;
        // rb.velocity = transform.forward * throwPower;
    }

    public void Retract()
    {
        if (GetComponent<Animator>()) GetComponent<Animator>().Play("Rotate");

        isRetracting = true;
        if (Vector3.Distance(hand.position, transform.position) < 0.09f)
        {
            Catch();
        }
        Vector3 directionToHand = hand.position - transform.position;
        rb.velocity = (directionToHand.normalized * retractPower);
    }

    public void Catch()
    {
        if (GetComponent<Animator>()) GetComponent<Animator>().Play("Idle");

        audiosource.PlayOneShot(catchAudio);
        isRetracting = false;
        isHeld = true;
        // rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        transform.position = hand.position;
        transform.parent = hand.transform;
        transform.rotation = hand.rotation;
    }

    public IEnumerator CheckForHeld()
    {
        yield return new WaitForSeconds(6);
        if (!isHeld)
        {
            Retract();
        }
        StartCoroutine(CheckForHeld());
    }
}
