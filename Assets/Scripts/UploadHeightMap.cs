using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Runtime.InteropServices;

public class UploadHeightMap : MonoBehaviour
{
    //Make sure to attach these Buttons in the Inspector
    public Button uploadHeightMapButton;
    [DllImport("__Internal")] private static extern void FocusFileUploader();

    void Start()
    {
        //Calls the TaskOnClick/TaskWithParameters/ButtonClicked method when you click the Button
        uploadHeightMapButton.onClick.AddListener(TaskOnClick);

    }

    void TaskOnClick()
    {
        //Output this to console when Button1 or Button3 is clicked
        Debug.Log("You have clicked the button!");
        FocusFileUploader();
    }
}