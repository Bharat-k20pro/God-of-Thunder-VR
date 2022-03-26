using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TechXR.Core.Sense;

public class TimeToClick : MonoBehaviour
{
    public float timeToClick;

    private void Update()
    {
        GazeTimer.Instance.TotalTime = timeToClick;
    }
}
