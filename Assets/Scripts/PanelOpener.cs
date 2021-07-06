using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelOpener : MonoBehaviour
{
    public GameObject menu;

    public void OpenPanel()
    {
        if (menu != null)
        {
            Animator animator = menu.GetComponent<Animator>();
            if(animator != null)
            {
                bool isOpen = animator.GetBool("OpenMenu");
                animator.SetBool("OpenMenu", !isOpen);
            }
        }
    }
}
