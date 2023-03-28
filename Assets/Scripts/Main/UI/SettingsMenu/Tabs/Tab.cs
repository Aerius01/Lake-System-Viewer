using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Tab : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private bool firstTab = false;
    [SerializeField] private List<Canvas> canvases;
    private Image background;
    
    private NewTabSelected newTabEvent;
    private bool _active = false;
    public bool active
    {
        get { return _active; }
        private set
        {
            _active = value;
            if (value) newTabEvent?.Invoke(this); // calls the TabController to ChangeTab
            this.panel.alpha = value ? 1f : 0f;
            this.panel.interactable = this.panel.blocksRaycasts = value;

            if (value)
            {
                this.background.color = ColorPicker.standardButtonColor;
                foreach (Canvas canvas in canvases) canvas.enabled = true; 
            }
            else 
            { 
                this.background.color = ColorPicker.disabledColor; 
                foreach (Canvas canvas in canvases) canvas.enabled = false; 
            }
        }
    }
    public TabController tabController { set { newTabEvent += value.ChangeTab; }}

    private void Awake() { background = this.GetComponent<Image>(); }
    private void Start() { this.active = this.firstTab;  }

    public void ButtonClick() { this.active = true; }
    public void Activate(bool status) { this.active = status; }
}