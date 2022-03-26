using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Portal : GazePointer
{
    // Start is called before the first frame update
    public GameObject plane;
    public Material sb2, gr2;
    public GameObject l2;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        l2.SetActive(true);
        PlayerManager.level++;
        RenderSettings.skybox = sb2;
        plane.GetComponent<Renderer>().material = gr2;
        gameObject.SetActive(false);
        // Destroy(gameObject);
        // por.SetActive(false);

    }
}
