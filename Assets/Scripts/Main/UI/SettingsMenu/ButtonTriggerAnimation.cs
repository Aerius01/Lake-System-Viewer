using UnityEngine;

public class ButtonTriggerAnimation : MonoBehaviour
{
    public Animator animator;
    public string boolName;
    public static bool menuOpen { get; private set;} = true;

    public virtual void ToggleBool()
    {
        menuOpen = !this.animator.GetBool(boolName);
        this.animator.SetBool(boolName, menuOpen);
    }
}
