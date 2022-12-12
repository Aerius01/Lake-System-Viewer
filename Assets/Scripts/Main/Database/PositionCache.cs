using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;

public class PositionCache
{
    private static int requeryLowerLimit = 100;
    public static int batchSize = 300;

    private DateTime earliestTimestamp { get { return this.backwardCache.Any() ? this.backwardCache[0].timestamp : this.forwardCache.Any() ? this.forwardCache[0].timestamp : DateTime.MaxValue; } }
    private DateTime latestTimestamp { get { return this.forwardCache.Any() ? this.forwardCache[^1].timestamp : DateTime.MinValue; } }
    public int fishID { get; private set; }

    public DateTime latestLiveQueryRequest { get; private set; }
    public DateTime latestIngameQueryRequest { get; private set; }

    private List<DataPacket> backwardCache, forwardCache;
    private readonly object locker = new object();

    public PositionCache(int fishID)
    {
        this.fishID = fishID;
        this.latestLiveQueryRequest = DateTime.MinValue;
        this.latestIngameQueryRequest = DateTime.MinValue;
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
                DatabaseConnection.QueuePositionBatchCommand(this.fishID, timestamp, forwardOnly:false);
                this.latestLiveQueryRequest = DateTime.Now;
                this.latestIngameQueryRequest = TimeManager.instance.currentTime;
                return null;
            }
            else if (DateTime.Compare(this.forwardCache[0].timestamp, timestamp) <= 0 && DateTime.Compare(timestamp, latestTimestamp) < 0)
            {
                // Debug.Log(string.Format("{0}: Position cache future packets", this.fishID));

                // Operate in future cached packets
                int currentIndex = forwardCache.BinarySearch(new DataPacket(0, timestamp, 0f, 0f, 0f), new TimestampComparer());

                // Recalibrate lists, culling previous entries
                if (currentIndex < 0)
                {
                    for (int i=0; i < ~currentIndex; i++)
                    {
                        if (backwardCache.Count >= PositionCache.requeryLowerLimit) backwardCache.RemoveAt(0);
                        backwardCache.Add(forwardCache[0]);
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
                // Operate in past cached packets
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
            if (forwardCache.Count < PositionCache.requeryLowerLimit) DatabaseConnection.QueuePositionBatchCommand(this.fishID, forwardCache[^1].timestamp);

            return new DataPacket[] { backwardCache[^1], forwardCache[0] };
        }
    }

    public Task AllocateNewPackets(List<DataPacket> listPackets, bool forwardOnly, CancellationToken token)
    {
        // Debug.Log(string.Format("{0}: Allocating new packets", this.fishID));
        if (forwardOnly) lock(this.locker) { foreach (DataPacket packet in listPackets) this.forwardCache.Add(packet); }
        else
        {
            int splitIndex = listPackets.BinarySearch(new DataPacket(0, TimeManager.instance.currentTime, 0f, 0f, 0f), new TimestampComparer()); 

            lock(this.locker)
            {    
                backwardCache = new List<DataPacket>();
                forwardCache = new List<DataPacket>();

                // Allocate packets based on the split
                if (splitIndex < 0)           
                {
                    // if we're already outside of the current time range, place one packet into forwardCache to reinitiate the querying process
                    if (~splitIndex == listPackets.Count) this.forwardCache.Add(listPackets[0]);
                    else
                    {
                        for (int i = 0; i < ~splitIndex; i++)
                        {
                            if (token.IsCancellationRequested) break;
                            else this.backwardCache.Add(listPackets[i]);
                        }
                        for (int i = ~splitIndex; i < listPackets.Count; i++)
                        {
                            if (token.IsCancellationRequested) break;
                            else this.forwardCache.Add(listPackets[i]);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i <= splitIndex; i++)
                    {
                        if (token.IsCancellationRequested) break;
                        else this.backwardCache.Add(listPackets[i]);
                    }
                    for (int i = splitIndex + 1; i < listPackets.Count; i++) 
                    {
                        if (token.IsCancellationRequested) break;
                        else this.forwardCache.Add(listPackets[i]);
                    }
                }
            }
        }

        // Debug.Log(string.Format("backward: {0}; forward: {1}", backwardCache.Count, forwardCache.Count));
        this.latestLiveQueryRequest = DateTime.MinValue;
        this.latestIngameQueryRequest = DateTime.MinValue;

        return Task.CompletedTask;
    }

    public void FullRequery(DateTime updateTime)
    { 
        DatabaseConnection.QueuePositionBatchCommand(this.fishID, updateTime, forwardOnly:false);
        this.latestLiveQueryRequest = DateTime.Now;
        this.latestIngameQueryRequest = TimeManager.instance.currentTime;
    }
}

public class TimestampComparer : Comparer<DataPacket>
{
    //https://stackoverflow.com/questions/58241526/binary-search-on-a-list-based-on-a-property-of-the-list-type
    public override int Compare(DataPacket x, DataPacket y) => x.timestamp.CompareTo(y.timestamp);
}