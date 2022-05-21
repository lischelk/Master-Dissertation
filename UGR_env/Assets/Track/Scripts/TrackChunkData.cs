using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LevelChunkData")]
public class TrackChunkData : ScriptableObject
{
    public enum Direction
    {
        left, right, straight 
    }

    public Vector2 chunkSize = new Vector2(40f, 40f);

    public GameObject[] levelChunks;
    public float exitDirection;

}