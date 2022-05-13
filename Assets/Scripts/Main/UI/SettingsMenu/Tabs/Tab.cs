using UnityEngine;
using UnityEngine.UI;

public class Tab : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private bool firstTab = false;
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

            if (value) { this.background.color = FlexibleColorPickerUtils.standardButtonColor; }
            else { this.background.color = FlexibleColorPickerUtils.disabledColor; }
        }
    }
    public TabController tabController { set { newTabEvent += value.ChangeTab; }}

    private void Awake() { background = this.GetComponent<Image>(); }
    private void Start() { if (firstTab) { this.active = true; } }

    public void ButtonClick() { this.active = true; }
    public void Activate(bool status) { this.active = status; }
}