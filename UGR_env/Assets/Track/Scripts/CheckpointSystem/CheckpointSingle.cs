using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{
    //private Track trackCheckpoints;
    public delegate void EnterAction();
    public static event EnterAction OnCheckpointEnter;
    private bool entered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out Player player) && !entered)
        {
            OnCheckpointEnter();
            entered = true;
            //trackCheckpoints.CarThroughCheckpoint(this, other.transform);
        }
    }

    public void SetTrackCheckpoints(Track trackCheckpoints)
    {
        //this.trackCheckpoints = trackCheckpoints;
    }
}