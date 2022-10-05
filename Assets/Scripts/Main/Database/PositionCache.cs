using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PositionCache
{
    private DateTime earliestTimestamp { get { return this.backwardCache.Any() ? this.backwardCache[0].timestamp : this.forwardCache.Any() ? this.forwardCache[0].timestamp : DateTime.MaxValue; } }
    private DateTime latestTimestamp { get { return this.forwardCache.Any() ? this.forwardCache[^1].timestamp : DateTime.MinValue; } }
    public int fishID { get; private set; }

    public bool morePackets { get { lock(this.locker) return this.forwardCache.Any(); } }
    public bool querySent { get; private set; }

    private List<DataPacket> backwardCache, forwardCache;
    private readonly object locker = new object();

    public PositionCache(int fishID)
    {
        this.fishID = fishID;
        this.querySent = false;
        this.backwardCache = new List<DataPacket>();
        this.forwardCache = new List<DataPacket>();
    }

    public DataPacket[] GetCachedBounds(DateTime timestamp)
    {
        lock(this.locker)
        {
            if (DateTime.Compare(earliestTimestamp, timestamp) > 0 || DateTime.Compare(timestamp, latestTimestamp) >= 0)
            {
                // Debug.Log(string.Format("{0}: Position cache full requery", this.fishID));
                // We are outside the current range
                backwardCache = new List<DataPacket>();
                forwardCache = new List<DataPacket>();

                DatabaseConnection.QueuePositionBatchCommand(this.fishID, timestamp, forwardOnly:false);
                this.querySent = true;
                return null;
            }
            else if (DateTime.Compare(this.forwardCache[0].timestamp, timestamp) <= 0 && DateTime.Compare(timestamp, latestTimestamp) < 0)
            {
                // Debug.Log(string.Format("{0}: Position cache future packets", this.fishID));

                // Operate in future packets
                int currentIndex = forwardCache.BinarySearch(new DataPacket(0, timestamp, 0f, 0f, 0f), new TimestampComparer());

                // Recalibrate lists, culling previous entries
                if (currentIndex < 0)
                {
                    for (int i=0; i < ~currentIndex; i++)
                    {
                        if (backwardCache.Count >= 100) backwardCache.RemoveAt(0);
                        try { backwardCache.Add(forwardCache[0]); }
                        catch (ArgumentOutOfRangeException e)
                        { 
                            // Debug.Log(e.Message);
                            // Debug.Log(string.Format("BITWISE INDEX: {0}; i: {1}; forwardCache len: {2} ", ~currentIndex, i, forwardCache.Count));
                        }
                        forwardCache.RemoveAt(0);
                    }
                }
                else
                {
                    for (int i=0; i <= currentIndex; i++)
                    {
                        backwardCache.RemoveAt(0);
                        backwardCache.Add(forwardCache[0]);
                        forwardCache.RemoveAt(0);
                    }
                }
            }
            else if (DateTime.Compare(earliestTimestamp, timestamp) <= 0 && DateTime.Compare(timestamp, this.backwardCache[^1].timestamp) < 0)
            {
                // Debug.Log(string.Format("{0}: Position cache previous packets", this.fishID));
                // If the timestamp is exactly equal to the last TS in backwardCache, then no changes to the lists are necessary
                // Operate in previous packets
                int currentIndex = backwardCache.BinarySearch(new DataPacket(0, timestamp, 0f, 0f, 0f), new TimestampComparer());

                // Recalibrate lists, without culling future entries (we'll likely use them again)
                if (currentIndex < 0)
                {
                    for (int i=backwardCache.Count-1; i >= ~currentIndex; i--)
                    {
                        forwardCache.Insert(0, backwardCache[i]);
                        backwardCache.RemoveAt(i);
                    }
                }
                else
                {
                    for (int i=backwardCache.Count-1; i > currentIndex; i--)
                    {
                        forwardCache.Insert(0, backwardCache[i]);
                        backwardCache.RemoveAt(i);
                    }
                }
            }

            // Queue another forward query to keep the cache full if the list is dwindling
            if (forwardCache.Count < 20) DatabaseConnection.QueuePositionBatchCommand(this.fishID, forwardCache[^1].timestamp);

            return new DataPacket[] { backwardCache[^1], forwardCache[0] };
        }
    }

    public void AllocateNewPackets(List<DataPacket> listPackets, bool forwardOnly)
    {
        // Debug.Log(string.Format("{0}: Allocating new packets", this.fishID));
        if (forwardOnly) lock(this.locker) { foreach (DataPacket packet in listPackets) this.forwardCache.Add(packet); }
        else
        {
            int splitIndex = listPackets.BinarySearch(new DataPacket(0, TimeManager.instance.currentTime, 0f, 0f, 0f), new TimestampComparer()); 

            lock(this.locker)
            {    
                // Allocate packets based on the split. The lists are already empty if we've run a double-sided query so populating is straightforward
                if (splitIndex < 0)           
                {
                    // if we're already outside of the current time range, place one packet into forwardCache to reinitiate the querying process
                    if (~splitIndex == listPackets.Count) this.forwardCache.Add(listPackets[0]);
                    else
                    {
                        for (int i = 0; i < ~splitIndex; i++) this.backwardCache.Add(listPackets[i]);
                        for (int i = ~splitIndex; i < listPackets.Count; i++) this.forwardCache.Add(listPackets[i]);
                    }
                }
                else
                {
                    for (int i = 0; i <= splitIndex; i++) this.backwardCache.Add(listPackets[i]);
                    for (int i = splitIndex + 1; i < listPackets.Count; i++) this.forwardCache.Add(listPackets[i]);
                }
            }
        }

        // Debug.Log(string.Format("backward: {0}; forward: {1}", backwardCache.Count, forwardCache.Count));
        this.querySent = false;
    }
}

public class TimestampComparer : Comparer<DataPacket>
{
    //https://stackoverflow.com/questions/58241526/binary-search-on-a-list-based-on-a-property-of-the-list-type
    public override int Compare(DataPacket x, DataPacket y) => x.timestamp.CompareTo(y.timestamp);
}