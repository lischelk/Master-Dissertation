using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tracks : MonoBehaviour
{
    [SerializeField] private List<Track> tracks;

    public List<Track> getTracks()
    {
        return tracks;
    }
}
