using System;
using System.Collections.Generic;
using System.IO;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

public class TrackLayoutGenerator : MonoBehaviour
{
    [Header("Settings")]
    public int chunksToSpawn;
    public int chunkFinishDefault = 8;
    private int chunkFinish;
    public int testSetSize = 20;
    private int trackCount;
    public Text Counter;
    [Header("Domain Randomizer")]
    public bool noise = true;
    public float coneNoiseAmount = 0.5f;

    public TrackChunkData[] trackChunkData;
    private TrackChunkData previousChunk;
    private List<GameObject> chunkList = new List<GameObject>();
    private List<(int, int)> chunkListIndex = new List<(int, int)>();

    private Vector3 spawnPosition;
    private Vector3 offsetPosition;
    private float offsetRotation;
    private float previousDirection;
    private int chunkCount;
    private Transform spawnpointInitial;

    private readonly System.Random random = new System.Random();

    public delegate void FinishAction();
    public static event FinishAction Finished;

    private void Awake()
    {
        chunkFinish = (int) Academy.Instance.EnvironmentParameters.GetWithDefault("track_length", chunkFinishDefault);
    }

    void OnEnable()
    {
        TriggerEnter.OnChunkEntered += IncreaseCount;
        trackCount = 0;
    }

    void IncreaseCount()
    {
        if (chunkCount + 1 == chunkFinish)
        {
            Finished();
        }
        if (chunkCount + 1 == chunkFinish * 2)
        {
            Finished();
            return;
        }
        if (chunkCount > 0)
        {
            chunkList[chunkCount - 1].SetActive(false);
        }
        if (chunkCount + chunksToSpawn < chunkFinish * 2 + 1)
        {
            chunkList[chunkCount + chunksToSpawn].SetActive(true);
        }
        chunkCount += 1;
    }

    void GenerateTrack(bool useTest = false)
    {
        spawnPosition = Vector3.zero;
        previousDirection = 0;
        bool firstSpawn = true;

        if (useTest)
        {
            if (!File.Exists("TestSets/TestSet_ " + chunkFinish.ToString() + "_" + testSetSize.ToString() + "/" + trackCount.ToString() + ".txt"))
                CreateTestSet(chunkFinish, testSetSize);
            string[] text = File.ReadAllLines("TestSets/TestSet_ " + chunkFinish.ToString() + "_" + testSetSize.ToString() + "/" + trackCount.ToString() + ".txt");
            foreach (string line in text)
            {
                string[] values = line.Split(new char[] { '_', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                chunkListIndex.Add((int.Parse(values[0]), int.Parse(values[1])));
            }
        } else
        {
            CreateRandomTrackIndices(chunkFinish);
        }

        for (int i = 0; i < chunkFinish * 2 + 1; i++)
        {
            TrackChunkData nextChunk;
            GameObject objectFromChunk;
            if (i < chunkFinish)
            {
                nextChunk = trackChunkData[chunkListIndex[i].Item1];
                objectFromChunk = nextChunk.levelChunks[chunkListIndex[i].Item2];
            }
            else if (i == chunkFinish * 2)
            {
                nextChunk = trackChunkData[chunkListIndex[0].Item1];
                objectFromChunk = nextChunk.levelChunks[chunkListIndex[0].Item2];
            }
            else
            {
                nextChunk = trackChunkData[chunkListIndex[i - chunkFinish].Item1];
                objectFromChunk = nextChunk.levelChunks[chunkListIndex[i - chunkFinish].Item2];
            }

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
            if (noise)
            {
                Transform cones = clone.transform.Find("Cones");
                int count = cones.childCount;
                for (int conepair = 0; conepair < count; conepair++)
                {
                    cones.GetChild(conepair).Find("TrafficConeAllBlue").localPosition += new Vector3((2f * (float)random.NextDouble() - 1f) * coneNoiseAmount, 0, (2f * (float)random.NextDouble() - 1f) * coneNoiseAmount);
                    cones.GetChild(conepair).Find("TrafficConeAllYellow").localPosition += new Vector3((2f * (float)random.NextDouble() - 1f) * coneNoiseAmount, 0, (2f * (float)random.NextDouble() - 1f) * coneNoiseAmount);
                }
            }
            chunkList.Add(clone);
            if (i == chunkFinish)
            {
                offsetPosition = spawnPosition;
                offsetRotation = previousDirection;
            }
            previousChunk = nextChunk;
            previousDirection += nextChunk.exitDirection;
            if (previousDirection >= 360) previousDirection -= 360;

            if (i == chunkFinish - 1 || i == chunkFinish * 2 - 1)
            {
                clone.transform.Find("Finish").gameObject.SetActive(true);
            }

            if (i > chunksToSpawn) clone.SetActive(false);

            firstSpawn = false;
        }
    }

    void DestroyTrack()
    {
        foreach (GameObject obj in chunkList)
        {
            Destroy(obj);
        }
        chunkList.Clear();
        chunkListIndex.Clear();
    }

    void ResetTrackChunks()
    {
        for (int i = 0; i < chunksToSpawn; i++)
        {
            chunkList[i].SetActive(true);
        }
        for (int i = chunksToSpawn; i < chunkList.Count; i++)
        {
            chunkList[i].SetActive(false);
        }
    }

    public (Vector3, Quaternion, Vector3, float) ResetTrack(bool reset = true, bool useTest = false)
    {
        if (reset)
        {
            chunkFinish = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("track_length", chunkFinishDefault);
            // Stop if no more test tracks to drive
            if (useTest && trackCount == testSetSize)
            {
/*#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                return (Vector3.zero, Quaternion.identity, Vector3.zero, 0f);
#endif*/
                trackCount = 0;
                
            }
            // Destory old track
            DestroyTrack();
            // Generate new track
            GenerateTrack(useTest);
            // Choose a checkpoint on first chunk to spawn on
            Transform spawnpoints = chunkList[0].transform.Find("Spawnpoints");
            //spawnpointInitial = spawnpoints.GetChild(random.Next(spawnpoints.childCount));
            spawnpointInitial = spawnpoints.GetChild(0);
            // Increase trackCount
            trackCount++;
            Counter.text = "Track: " + trackCount.ToString();
        } else
        {
            // Make the first chunks (chunksToSpawn) active, rest inactive
            ResetTrackChunks();
        }

        chunkCount = 0;

        return (spawnpointInitial.position, spawnpointInitial.rotation, offsetPosition, offsetRotation);
    }

    public void CreateTestSet(int trackSize, int setSize)
    {
        for (int j = 0; j < setSize; j++)
        {
            List<string> indices = new List<string>();
            for (int i = 0; i < trackSize; i++)
            {
                int nextChunkIndex = random.Next(trackChunkData.Length);
                int objectFromChunkIndex = random.Next(trackChunkData[nextChunkIndex].levelChunks.Length);
                indices.Add(nextChunkIndex.ToString() + "_" + objectFromChunkIndex.ToString());

            }
            Directory.CreateDirectory("TestSets/TestSet_ " + trackSize.ToString() + "_" + setSize.ToString() + "/");
            File.WriteAllLines("TestSets/TestSet_ " + trackSize.ToString() + "_" + setSize.ToString() + "/" + j.ToString() + ".txt", indices);
        }
    }
    public void CreateRandomTrackIndices(int trackSize)
    {
        for (int i = 0; i < trackSize; i++)
        {
            int nextChunkIndex = random.Next(trackChunkData.Length);
            int objectFromChunkIndex = random.Next(trackChunkData[nextChunkIndex].levelChunks.Length);
            chunkListIndex.Add((nextChunkIndex, objectFromChunkIndex));
        }
    }
}
