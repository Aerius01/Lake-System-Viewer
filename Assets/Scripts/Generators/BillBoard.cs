using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BillBoard : MonoBehaviour
{
    private Camera mainCam;

    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.LookAt(mainCam.transform);    
    }
}
