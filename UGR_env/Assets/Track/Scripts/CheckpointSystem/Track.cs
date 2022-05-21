using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Track : MonoBehaviour
{
    public event EventHandler<CarCheckpointEventArgs> OnCarCorrectCheckpoint;
    //public event EventHandler<CarCheckpointEventArgs> OnCarWrongCheckpoint;

    public class CarCheckpointEventArgs : EventArgs
    {
        public Transform carTransform;
        public CarCheckpointEventArgs(Transform car)
        {
            carTransform = car;
        }
    }

    [SerializeField] private List<Transform> carTransformList;
    [SerializeField] private int counterMax = 20;

    private List<CheckpointSingle> checkpointList = new List<CheckpointSingle>();
    private List<int> nextCheckpointIndex = new List<int>();
    private System.Random random = new System.Random();
    private CheckpointSingle spawnPoint;
    private int spawnPointIndex;
    private bool start;
    private int direction;


    private void Awake()
    {
        Transform checkpointTransform = transform.Find("Checkpoints");

        foreach (Transform checkpointSingleTransform in checkpointTransform)
        {
            CheckpointSingle checkpointSingle = checkpointSingleTransform.GetComponent<CheckpointSingle>();
            checkpointSingle.SetTrackCheckpoints(this);
            checkpointList.Add(checkpointSingle);

        }
        nextCheckpointIndex = new List<int>(new int[carTransformList.Count]);
    }

    public void CarThroughCheckpoint(CheckpointSingle checkpointSingle, Transform carTransform)
    {
        if (start && checkpointSingle == spawnPoint) { return; }
        OnCarCorrectCheckpoint?.Invoke(this, new CarCheckpointEventArgs(carTransform));
        /*
        int nextCheckPointSingleIndex = nextCheckpointIndex[carTransformList.IndexOf(carTransform)];
        if (start && checkpointSingle == spawnPoint){return;}
        start = false;
        if (checkpointList.IndexOf(checkpointSingle) == nextCheckPointSingleIndex)
        {
            // Correct Checkpoint
            Debug.Log("Correct Checkpoint");
            int nextIndex = (nextCheckPointSingleIndex + direction) % checkpointList.Count;
            if (nextIndex < 0) {
                nextIndex = checkpointList.Count-1;
            }
            nextCheckpointIndex[carTransformList.IndexOf(carTransform)] = nextIndex;
            OnCarCorrectCheckpoint?.Invoke(this, new CarCheckpointEventArgs(carTransform));
        } else 
        {
            // Wrong Checkpoint
            Debug.Log("Wrong Checkpoint");
            Debug.Log(checkpointList.IndexOf(checkpointSingle) - nextCheckPointSingleIndex);
            OnCarWrongCheckpoint?.Invoke(this, new CarCheckpointEventArgs(carTransform));
        }*/
    }

    public (Vector3, Vector3) ResetCheckpoint(Transform carTransform, int counter)
    {
        start = true;
        if (counter % counterMax == 0)
        {
            Debug.Log("switching Position");
            spawnPointIndex = random.Next(checkpointList.Count);
            spawnPoint = checkpointList[spawnPointIndex];
            direction = 1 - random.Next(2) * 2;
        }

        int nextIndex = (spawnPointIndex + direction) % checkpointList.Count;
        if (nextIndex < 0)
        {
            nextIndex = checkpointList.Count - 1;
        }

        nextCheckpointIndex[carTransformList.IndexOf(carTransform)] = nextIndex;

        return (spawnPoint.transform.position - new Vector3(0,2,0), direction*spawnPoint.transform.forward);
    }
}

