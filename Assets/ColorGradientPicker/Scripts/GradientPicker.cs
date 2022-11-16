using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class GradientPicker : MonoBehaviour
{
    /// <summary>
    /// Event that gets called by the GradientPicker.
    /// </summary>
    /// <param name="g">received Gradient</param>
    public delegate void GradientEvent(Gradient g);

    private static GradientPicker _instance;
    [HideInInspector]
    public static GradientPicker instance {get { return _instance; } set {_instance = value; }}


    //onGradientSelected Event
    private static GradientEvent onGS;

    //Gradient before editing
    private Gradient originalGradient;
    //current Gradient
    private Gradient modifiedGradient;

    private static Gradient original;

    //key template
    private GameObject key;

    private static bool interact;


    //all these objects only work on Prefab
    private TMP_InputField positionComponent;
    private Image colorComponent;

    private List<Slider> colorKeyObjects;
    private List<GradientColorKey> colorKeys;
    private int selectedColorKey;
    private List<GradientAlphaKey> alphaKeys;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        key = transform.GetChild(1).gameObject;
        positionComponent = transform.parent.GetChild(1).GetComponent<TMP_InputField>();
        colorComponent = transform.parent.GetChild(2).GetComponent<Image>();
        transform.parent.gameObject.SetActive(false);
    }
    /// <summary>
    /// Creates a new GradiantPicker
    /// </summary>
    /// <param name="original">Color before editing</param>
    /// <param name="onGradientChanged">Event that gets called when the gradient gets modified</param>
    /// <param name="onGradientSelected">Event that gets called when one of the buttons done or cancel gets pressed</param>
    /// <returns>False if the instance is already running</returns>
    public static bool Create(Gradient original, GradientEvent onGradientSelected)
    {
        if (instance is null)
        {
            Debug.LogError("No Gradientpicker prefab active on 'Start' in scene");
            return false;
        }
        else
        {
            GradientPicker.original = original;
            instance.originalGradient = new Gradient();
            instance.originalGradient.SetKeys(original.colorKeys, original.alphaKeys);
            instance.modifiedGradient = new Gradient();
            instance.modifiedGradient.SetKeys(original.colorKeys, original.alphaKeys);
            onGS = onGradientSelected;
            instance.transform.parent.gameObject.SetActive(true);
            instance.Setup();
            return true;
        }
    }
    //Setup new GradientPicker
    private void Setup()
    {
        interact = false;
        instance.colorKeyObjects = new List<Slider>();
        instance.colorKeys = new List<GradientColorKey>();
        instance.alphaKeys = new List<GradientAlphaKey>();

        instance.originalGradient = new Gradient();
        instance.originalGradient.SetKeys(GradientPicker.original.colorKeys, GradientPicker.original.alphaKeys);
        instance.modifiedGradient = new Gradient();
        instance.modifiedGradient.SetKeys(GradientPicker.original.colorKeys, GradientPicker.original.alphaKeys);

        foreach (GradientColorKey k in GradientPicker.original.colorKeys) CreateColorKey(k);

        CalculateTexture();
        interact = true;
    }
    //creates a ColorKey UI object
    private void CreateColorKey(GradientColorKey k)
    {
        if (instance.colorKeys.Count < 8)
        {
            Slider s = Instantiate(key, transform.position, new Quaternion(), transform).GetComponent<Slider>();
            ((RectTransform)s.transform).anchoredPosition = new Vector2(0, -29f);
            s.name = string.Format("ColorKey{0}", instance.colorKeys.Count);
            s.gameObject.SetActive(true);
            s.value = k.time;
            s.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().color = k.color;
            instance.colorKeyObjects.Add(s);
            instance.colorKeys.Add(k);
            ChangeSelectedColorKey(instance.colorKeys.Count - 1);
        }
    }
    //checks if new ColorKey should be created
    public void CreateNewColorKey(float time)
    {
        if (Input.GetMouseButtonDown(0))
        {
            interact = false;
            CreateColorKey(new GradientColorKey(instance.modifiedGradient.Evaluate(time), time));
            interact = true;
        }
    }

    private void CalculateTexture()
    {
        Color[] g = new Color[325];
        for (int i = 0; i < g.Length; i++)
        {
            g[i] = instance.modifiedGradient.Evaluate(i / (float)g.Length);
        }
        Texture2D tex = new Texture2D(g.Length, 1)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        tex.SetPixels(g);
        tex.Apply();
        GetComponent<RawImage>().texture = tex;
    }

    private void ChangeSelectedColorKey(int value)
    {
        if (instance.colorKeyObjects.Count() > selectedColorKey)
        {
            instance.colorKeyObjects[selectedColorKey].transform.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.gray;
        }
        instance.colorKeyObjects[value].transform.GetChild(0).GetChild(0).GetComponent<Image>().color = Color.green;
        if (selectedColorKey != value && !ColorPickerImported.done)
        {
            ColorPickerImported.Done();
        }
        selectedColorKey = value;
        instance.colorKeyObjects[value].Select();
    }

    //checks if Key can be deleted
    public void CheckDeleteKey(Slider s)
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (s.name.Contains("ColorKey") && instance.colorKeys.Count > 2)
            {
                if (!ColorPickerImported.done)
                {
                    ColorPickerImported.Done();
                    return;
                }
                int index = instance.colorKeyObjects.IndexOf(s);
                Destroy(instance.colorKeyObjects[index].gameObject);
                instance.colorKeyObjects.RemoveAt(index);
                instance.colorKeys.RemoveAt(index);
                if (index <= selectedColorKey) ChangeSelectedColorKey(selectedColorKey - 1);
                instance.modifiedGradient.SetKeys(instance.colorKeys.ToArray(), instance.alphaKeys.ToArray());
                CalculateTexture();
            }
        }
    }
    //changes Selected Key
    public void Select()
    {
        Slider s = EventSystem.current.currentSelectedGameObject.GetComponent<Slider>();
        s.transform.SetAsLastSibling();
        if (s.name.Contains("ColorKey"))
        {
            ChangeSelectedColorKey(instance.colorKeyObjects.IndexOf(s));
            colorComponent.gameObject.SetActive(true);
            positionComponent.text = Mathf.RoundToInt(instance.colorKeys[selectedColorKey].time * 100f).ToString();
            colorComponent.GetComponent<Image>().color = instance.colorKeys[selectedColorKey].color;
        }
    }
    //accessed by position Slider
    public void SetTime(float time)
    {
        if (interact)
        {
            Slider s = EventSystem.current.currentSelectedGameObject.GetComponent<Slider>();
            if (s.name.Contains("ColorKey"))
            {
                int index = instance.colorKeyObjects.IndexOf(s);
                instance.colorKeys[index] = new GradientColorKey(instance.colorKeys[index].color, time);
            }
            instance.modifiedGradient.SetKeys(instance.colorKeys.ToArray(), instance.alphaKeys.ToArray());
            CalculateTexture();
            positionComponent.text = Mathf.RoundToInt(time * 100f).ToString();
        }
    }
    //accessed by position InputField
    public void SetTime()
    {
        interact = false;
        float t = Mathf.Clamp(float.Parse(positionComponent.text), 0, 100) * 0.01f;
        if (colorComponent.gameObject.activeSelf)
        {
            instance.colorKeyObjects[selectedColorKey].value = t;
            instance.colorKeys[selectedColorKey] = new GradientColorKey(instance.colorKeys[selectedColorKey].color, t);
        }
        instance.modifiedGradient.SetKeys(instance.colorKeys.ToArray(), instance.alphaKeys.ToArray());
        CalculateTexture();
        interact = true;
    }
    //choose color button call
    public void ChooseColor()
    {
        ColorPickerImported.Create(instance.colorKeys[selectedColorKey].color, "Gradient Color Key", (c) => UpdateColor(selectedColorKey, c), null);
    }

    private void UpdateColor(int index, Color c)
    {
        interact = false;
        instance.colorKeys[index] = new GradientColorKey(c, instance.colorKeys[index].time);
        instance.colorKeyObjects[index].transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().color = c;
        colorComponent.color = c;
        instance.modifiedGradient.SetKeys(instance.colorKeys.ToArray(), instance.alphaKeys.ToArray());
        CalculateTexture();
        interact = true;
    }

    //cancel button call
    public void CCancel()
    {
        foreach (Slider s in instance.colorKeyObjects) Destroy(s.gameObject);

        instance.Setup();
    }

    //done button call
    public void CDone()
    {
        foreach (Slider s in instance.colorKeyObjects) Destroy(s.gameObject);

        GradientPicker.original = new Gradient();
        GradientPicker.original.SetKeys(instance.modifiedGradient.colorKeys, instance.modifiedGradient.alphaKeys);

        onGS?.Invoke(instance.modifiedGradient);
        instance.Setup();
    }
}
