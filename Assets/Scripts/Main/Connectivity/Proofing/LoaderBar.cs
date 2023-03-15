using Unity;
using UnityEngine;
using System;
using TMPro;

public class LoaderBar : MonoBehaviour
{
    // Apparently "LoadingBar" already exists in the global namespace

    [SerializeField] private TextMeshProUGUI tableText;
    [SerializeField] private RectTransform maskImage, dynamicImage;

    private float periodCounter = 0f, barIncrement;
    private string currentText = "";

    private int totalTableCount = 0, processedTableCount = 0;

    public void WakeUp(int tableCount)
    {
        if (!this.gameObject.activeSelf) this.gameObject.SetActive(true);
        this.totalTableCount = tableCount;
        this.barIncrement = maskImage.rect.width / tableCount;
    }

    public void ShutDown()
    {
        this.gameObject.SetActive(false);
        this.processedTableCount = 0;
        this.dynamicImage.sizeDelta -= new Vector2(this.dynamicImage.rect.width, 0f);
    }

    public void SetText(string text)
    {  
        this.periodCounter = 0f;
        this.currentText = text;
        this.tableText.text = this.currentText;

        this.processedTableCount += 1;
        this.dynamicImage.sizeDelta += new Vector2(this.barIncrement, 0f);
    }

    private void Update()
    {
        if (this.totalTableCount != 0)
        {
            // Oscillating period at end of table name
            this.periodCounter += Time.deltaTime;

            if (this.periodCounter >= 4f) this.periodCounter = 0f; 
            else if (this.periodCounter >= 3f) if (this.currentText.Substring(this.currentText.Length - 3) != "...") { this.currentText = this.currentText + "."; this.tableText.text = this.currentText; }
            else if (this.periodCounter >= 2f) if (this.currentText.Substring(this.currentText.Length - 2) != "..") { this.currentText = this.currentText + "."; this.tableText.text = this.currentText; }
            else if (this.periodCounter >= 1f) if (this.currentText.Substring(this.currentText.Length - 1) != ".") { this.currentText = this.currentText + ".";  this.tableText.text = this.currentText; }
            else if (this.periodCounter >= 0f) if (this.currentText.Substring(this.currentText.Length - 3) == "...") { this.currentText = this.currentText.Substring(0, this.currentText.Length - 3); this.tableText.text = this.currentText; }
        }
    }
}
