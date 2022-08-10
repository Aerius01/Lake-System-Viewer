using Unity;
using UnityEngine;
using System;

// Support class
public class DataPacket
{
    public int id {get; private set;}
    public DateTime timestamp {get; private set;}
    public Vector3 pos {get; private set;}

    public DataPacket(int id, DateTime timestamp, float x, float y, float z)
    {
        this.id = id;
        this.timestamp = timestamp;
        this.pos = new Vector3(x, y, z);
    }
}