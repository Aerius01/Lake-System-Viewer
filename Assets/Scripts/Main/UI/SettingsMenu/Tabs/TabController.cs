using UnityEngine;

public delegate void NewTabSelected(Tab tab);

public class TabController : MonoBehaviour
{
    [SerializeField] private Tab[] tabs;
    private static bool changeTab;

    private void Awake() { TabController.changeTab = false; foreach (Tab tab in tabs) { tab.tabController = this; } }
    public void ChangeTab(Tab newTab) { foreach (Tab tab in tabs) { if (tab != newTab) tab.Activate(false); } }

    private void Update() { if (TabController.changeTab) { TabController.changeTab = false; this.tabs[1].ButtonClick(); } }
    public static void TriggerTabChange() { TabController.changeTab = true; }
}
