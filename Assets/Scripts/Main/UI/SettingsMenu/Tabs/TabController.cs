using UnityEngine;

public delegate void NewTabSelected(Tab tab);

public class TabController : MonoBehaviour
{
    [SerializeField] private Tab[] tabs;

    private void Awake() { foreach (Tab tab in tabs) { tab.tabController = this; } }
    public void ChangeTab(Tab newTab) { foreach (Tab tab in tabs) { if (tab != newTab) tab.Activate(false); } }
    
}
