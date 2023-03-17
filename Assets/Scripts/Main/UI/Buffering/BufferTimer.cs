using System;
public class BufferTimer
{
    private DateTime startTime;
    public bool buffering
    {
        get
        {
            if ((DateTime.Now - startTime).TotalSeconds > 0.5f) return true;
            else return false;
        }
    }
    public bool stop { get; private set; }


    public BufferTimer() { this.startTime = DateTime.Now; this.stop = false; }
    public void StopTiming() { this.stop = true; }
}
