using UnityEngine;

public class ButtonTriggerAnimation : MonoBehaviour
{
    public Animator animator;
    public string boolName;
    // Start is called before the first frame update
    public virtual void ToggleBool()
    {
        this.animator.SetBool(boolName,!this.animator.GetBool(boolName));
    }
}
