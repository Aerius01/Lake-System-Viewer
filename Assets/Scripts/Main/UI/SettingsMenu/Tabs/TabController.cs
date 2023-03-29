using UnityEngine;
using System.Collections.Generic;

public class TabController : MonoBehaviour
{
    [SerializeField] private List<Tab> tabs;
    [SerializeField] private int firstTab = 0;
    private Tab activeTab;

    private static TabController _instance;
    [HideInInspector] public static TabController instance {get { return _instance; } set {_instance = value; }}

    private void Awake() 
    { 
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }
    }
    
    private void Start()
    {
        foreach (Tab tab in this.tabs)
        { 
            if (tab == this.tabs[this.firstTab]) 
            {
                this.activeTab = tab; 
                tab.ActivateTab(true);
            }
            else { tab.ActivateTab(false); }
        } 
    }

    public void NewTab(Tab newTab)
    {
        foreach (Tab tab in this.tabs)
        {
            if (tab == newTab) { tab.ActivateTab(true); }
            else if (tab.active) { tab.ActivateTab(false); }
        }
    }

    public void FishTab() { this.NewTab(this.tabs[1]); }
}
