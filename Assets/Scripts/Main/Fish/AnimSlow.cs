using UnityEngine;

public class AnimSlow : MonoBehaviour
{
    private void Start()
    {
        Animation anim = this.GetComponent<Animation>();
        foreach (AnimationState state in anim)
        {
            state.speed = 0.5F;
        }
    }
}
