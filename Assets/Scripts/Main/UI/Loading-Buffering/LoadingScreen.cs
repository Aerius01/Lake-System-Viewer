using UnityEngine;
using TMPro;

public class LoadingScreen : MonoBehaviour
{
    private static CanvasGroup mainCanvas;
    private static GameObject loadingScreen;
    private static TextMeshProUGUI textField;

    private void Awake()
    {
        LoadingScreen.loadingScreen = this.gameObject;
        LoadingScreen.mainCanvas = this.transform.parent.GetComponent<CanvasGroup>();
        LoadingScreen.textField = this.gameObject.GetComponentInChildren<TextMeshProUGUI>();

        LoadingScreen.Deactivate();
    }

    public static void Activate(bool activation=true)
    {
        LoadingScreen.loadingScreen.SetActive(activation);
        LoadingScreen.mainCanvas.interactable = !activation;
    }

    public static void Deactivate() { LoadingScreen.Activate(false); }

    public static void Text(string newText) { LoadingScreen.textField.text = newText; }
}