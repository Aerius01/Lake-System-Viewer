using UnityEngine;
using System;
using System.Collections.Generic;

public class Buffer : MonoBehaviour
{
    [SerializeField] private GameObject bufferIcon;

    private static List<BufferTimer> listOfTimers;
    private static bool alreadyActive = false;
    private static readonly object locker = new object();


    private void Awake() { Buffer.listOfTimers = new List<BufferTimer>(); }

    public static BufferTimer InitializeTimer() 
    {
        BufferTimer newTimer = new BufferTimer();
        lock(Buffer.locker) { Buffer.listOfTimers.Add(newTimer); }
        return newTimer;
    }

    private void Update()
    {
        if (Buffer.listOfTimers.Count > 0)
        {
            bool showIcon = false;
            lock(Buffer.locker)
            {
                for (int i = Buffer.listOfTimers.Count - 1; i >= 0 ; i--)
                {
                    if (Buffer.listOfTimers[i].stop) Buffer.listOfTimers.RemoveAt(i);
                    else if (Buffer.listOfTimers[i].buffering) showIcon = true;

                    if (showIcon) break;
                }
            }

            if (showIcon && !Buffer.alreadyActive)
            {
                this.bufferIcon.SetActive(true);
                Buffer.alreadyActive = true;
            }
        }
        else if (this.bufferIcon.activeSelf)
        {
            this.bufferIcon.SetActive(false);
            Buffer.alreadyActive = false;
        }
    }
}