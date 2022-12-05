using UnityEngine;
using UnityEngine.UI;

public class ToggleGrouping : MonoBehaviour
{
    // Toggles that are grouped together cannot be simultaneously selected, but one must always be selected
    // Activating one deactivates the others, and deactivating the active toggle activates the first in the group that isn't the deactivated toggle
    
    [SerializeField] private Toggle[] toggles;
    private Toggle currentToggle;

    private void Awake()
    {
        foreach (Toggle toggle in toggles)
        {
            if (toggle.isOn)
            {
                this.currentToggle = toggle;
                break;
            }
        }
    }

    public void ChangeToggle()
    {
        int count = 0;
        foreach (Toggle toggle in toggles) if (toggle.isOn) count++;

        if (count == 0)
        {
            // Activate the first toggle that isn't the unchecked one
            foreach (Toggle toggle in toggles)
            {
                if (toggle != this.currentToggle)
                {
                    toggle.isOn = true;
                    this.currentToggle = toggle;
                    break;
                }
            }
        }
        else
        {
            foreach (Toggle toggle in toggles)
            {
                if (toggle.isOn && toggle != this.currentToggle)
                {
                    this.currentToggle.isOn = false;
                    this.currentToggle = toggle;
                    break;
                }
            }
        }
    }
}
