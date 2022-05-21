using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerEnter : MonoBehaviour
{
    public delegate void EnterAction();
    public static event EnterAction OnChunkEntered;

    private void OnTriggerEnter(Collider other)
    {
        Player carTag = other.GetComponent<Player>();
        if (carTag != null)
        {
            OnChunkEntered();
        }
    }
}