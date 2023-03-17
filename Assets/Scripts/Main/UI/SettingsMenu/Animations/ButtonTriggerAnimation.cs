using UnityEngine;
using System.Collections;

public delegate void MenuChange();

public class ButtonTriggerAnimation : MonoBehaviour
{
    public Animator animator;
    public AnimationClip clip;
    public string boolName;
    public bool menuClosed { get { return this.animator.GetBool(boolName); } }
    public float menuWidth { get { return this.menuClosed ? 0f : this.gameObject.GetComponent<RectTransform>().rect.width ; } }
    public event MenuChange menuChange;

    public virtual void ToggleBool()
    {
        this.animator.SetBool(boolName, !menuClosed);
        if (boolName == "trigger") StartCoroutine(DelayedEvent());
    }

    private IEnumerator DelayedEvent()
    {
        yield return new WaitForSeconds(clip.length);
        menuChange?.Invoke();
    }
}
