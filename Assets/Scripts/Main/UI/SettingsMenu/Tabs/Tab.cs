using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Tab : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private List<Canvas> canvases;
    [SerializeField] private Image background;
    
    private bool _active = false;
    public bool active
    {
        get { return _active; }
        private set
        {
            _active = value;
            this.panel.alpha = value ? 1f : 0f;
            this.panel.interactable = this.panel.blocksRaycasts = value;

            if (value)
            {
                this.background.color = ColorPicker.standardButtonColor;
                foreach (Canvas canvas in this.canvases) { canvas.enabled = true;}

            }
            else 
            { 
                this.background.color = ColorPicker.disabledColor; 
                foreach (Canvas canvas in this.canvases) { canvas.enabled = true;}
            }
        }
    }

    public void ButtonClick() { TabController.instance.NewTab(this); }
    public void ActivateTab(bool status) { this.active = status; }

}