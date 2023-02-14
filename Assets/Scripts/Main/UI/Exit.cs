using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class Exit : MonoBehaviour 
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button xButton, closeButton;


    private void Awake() { this.OnDeselect(); }

    public void XPress()
    {
        this.canvasGroup.interactable = true;
        this.canvasGroup.alpha = 1f;
        this.xButton.interactable = false;

        this.closeButton.Select();
    }

    public void OnDeselect()
    {
        // Disable confirmations
        this.canvasGroup.interactable = false;
        this.canvasGroup.alpha = 0f;

        // Enable normal button
        this.xButton.interactable = true;
    }

    public void CloseButton() { Application.Quit(); }
}