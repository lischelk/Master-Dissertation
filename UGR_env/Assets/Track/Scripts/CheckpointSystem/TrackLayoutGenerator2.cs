using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackLayoutGenerator2 : MonoBehaviour
{
    [Header("Settings")]
    public int chunksToSpawn;
    public int chunkFinish;

    public TrackChunkData[] trackChunkData;
    private TrackChunkData previousChunk;
    private Queue<GameObject> chunkQueue = new Queue<GameObject>();

    private bool firstSpawn;
    private Vector3 spawnPosition;
    private float previousDirection;

    private int chunkCount;

    private readonly System.Random random = new System.Random();

    public delegate void FinishAction();
    public static event FinishAction Finished;


    void OnEnable()
    {
        TriggerEnter.OnChunkEntered += PickAndSpawnChunk;
        TriggerEnter.OnChunkEntered += DestroyChunk;
        TriggerEnter.OnChunkEntered += IncreaseCount;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            PickAndSpawnChunk();
        }
    }

    void IncreaseCount()
    {
        chunkCount += 1;
    }

    void PickAndSpawnChunk()
    {
        if (chunkCount == chunkFinish)
        {
            Finished();
            return;
        }

        if (chunkCount + chunksToSpawn <= chunkFinish + 1)
        {
            TrackChunkData nextChunk = trackChunkData[random.Next(trackChunkData.Length)];
            GameObject objectFromChunk = nextChunk.levelChunks[random.Next(nextChunk.levelChunks.Length)];

            if (!firstSpawn)
            {
                switch (previousDirection)
                {
                    case 0:
                        spawnPosition += new Vector3(0f, 0f, previousChunk.chunkSize.y);
                        break;
                    case 90:
                        spawnPosition += new Vector3(previousChunk.chunkSize.y, 0f, 0f);
                        break;
                    case 180:
                        spawnPosition += new Vector3(0f, 0f, -previousChunk.chunkSize.y);
                        break;
                    case 270:
                        spawnPosition += new Vector3(-previousChunk.chunkSize.y, 0f, 0f);
                        break;
                    default:
                        break;
                }
            }

            GameObject clone = Instantiate(objectFromChunk, spawnPosition, Quaternion.Euler(0, previousDirection, 0));
            chunkQueue.Enqueue(clone);
            previousChunk = nextChunk;
            previousDirection += nextChunk.exitDirection;
            if (previousDirection >= 360) previousDirection -= 360;

            if (chunkCount + chunksToSpawn == chunkFinish)
            {
                clone.transform.Find("Finish").gameObject.SetActive(true);
            }
        }
        firstSpawn = false;
    }

    void DestroyChunk()
    {
        if (chunkQueue.Count > chunksToSpawn+1)
        {
            GameObject obj = chunkQueue.Dequeue();
            Destroy(obj);
        }
    }

    public (Vector3, Quaternion) Reset(bool keep = false)
    {
        // Remove all old chuncks
        while (chunkQueue.Count > 0)
        {
            GameObject obj = chunkQueue.Dequeue();
            Destroy(obj);
        }

        chunkCount = 0;
        spawnPosition = new Vector3(0, 0, 0);
        previousDirection = 0;

        // Create a random First chunk
        /*TrackChunkData firstChunk = trackChunkData[random.Next(trackChunkData.Length)];
        GameObject objectFromChunk = firstChunk.levelChunks[random.Next(firstChunk.levelChunks.Length)];
        GameObject clone = Instantiate(objectFromChunk, spawnPosition, Quaternion.identity);
        chunkQueue.Enqueue(clone);
        previousChunk = firstChunk;
        previousDirection = firstChunk.exitDirection;*/
        firstSpawn = true;
        // Create a few extra chunks as buffer
        while (chunkQueue.Count < chunksToSpawn)
        {
            PickAndSpawnChunk();
        }

        // Choose a checkpoint on first chunk to spawn on
        Transform spawnpoints = chunkQueue.Peek().transform.Find("Spawnpoints");
        Transform spawnpoint = spawnpoints.GetChild(random.Next(spawnpoints.childCount));

        return (spawnpoint.position, spawnpoint.rotation);
    }
}
