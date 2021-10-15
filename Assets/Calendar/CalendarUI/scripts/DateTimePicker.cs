using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DateTimePicker : MonoBehaviour
{
    public DatePicker datePicker;
    public TimePicker timePicker;
    [SerializeField] private GameObject buttonObject;
    [SerializeField] private bool startTime;

    public DateTime? SelectedDateTime(){
        var d = datePicker.SelectedDate;
        var t = timePicker.SelectedTime();
        if (d != null){
            return new DateTime(d.Value.Year, d.Value.Month, d.Value.Day,
                            t.Hour, t.Minute, t.Second);
        }
        return null;
    }

    public void GetSelectedDate(){
        buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = ((DateTime)SelectedDateTime()).ToString("dd.MM.yyyy HH:mm:ss");

        // Set the filters for the PositionUploadTable class
        if (startTime)
        {
            PositionUploadTable.startCutoff = (DateTime)SelectedDateTime();
        }
        else
        {
            PositionUploadTable.endCutoff = (DateTime)SelectedDateTime();
        }
    }

    public void SetSelectedDateTime(DateTime value){
        datePicker.SetSelectedDate(value);
        timePicker.SetSelectedTime(value);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
