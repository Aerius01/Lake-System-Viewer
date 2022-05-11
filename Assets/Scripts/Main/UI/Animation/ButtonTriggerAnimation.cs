using UnityEngine;

public class ButtonTriggerAnimation : MonoBehaviour
{
    public Animator animator;
    public string boolName;
    public virtual void ToggleBool() { this.animator.SetBool(boolName,!this.animator.GetBool(boolName)); }
}
