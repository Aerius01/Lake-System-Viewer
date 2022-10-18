using UnityEngine;

public class FlipArrow : MonoBehaviour
{
    [SerializeField] private RectTransform buttonImage = null;
    public void FlipArrowButton() { buttonImage.localRotation = this.GetComponent<ButtonTriggerAnimation>().menuClosed ? Quaternion.Euler(new Vector3(0f, 0f, 0f)) : Quaternion.Euler(new Vector3(0f, 0f, 180f)); }
}
