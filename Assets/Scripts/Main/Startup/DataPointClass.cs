using System;

public class DataPointClass
{
    public float x, y, z;
    public DateTime obsTime { get; set; }

    public DataPointClass(float x, float y, float z, DateTime obsTime)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.obsTime = obsTime;
    }
}