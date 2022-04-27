using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ListBox : MonoBehaviour
{
    public bool open {get; protected set;} = false;
    public bool active {get { return activeToggle.isOn; }}
    public bool colored {get; protected set;} = false;

    [SerializeField]
    protected Toggle activeToggle;
}

public class FishBox : ListBox
{
    public Fish fish {get; private set;}
    public float contentSize
    {
        get
        {
            if (this.open) return 60f;
            else return 20f;
        }
    }

    public int rank {get; private set;}

    // Fish utility booleans
    private bool canvas {get { return fish.canvasActive; }} 
    private bool depthLine {get { return fish.depthLineActive; }} 
    private bool trail {get { return fish.trailActive; }} 
    private bool thermo {get { return fish.thermoIndActive; }} 

    // METHODS
    public void Activate(bool state=true)
    {
        activeToggle.isOn = state;
        // deactivate the fish
    }

    public void SetRank(int rank) { this.rank = rank; }
}

public class SpeciesBox : ListBox
{
    private List<FishBox> components;
    private int currentRank = 0;
    private string speciesName;
    public float contentSize
    {
        get
        {
            float totalSize = 0f;
            foreach (FishBox box in components) { totalSize += box.contentSize; }
            return totalSize;
        }
    }

    // METHODS
    public SpeciesBox(string name) { this.speciesName = name; components = new List<FishBox>();}

    public void Add(FishBox box) { box.SetRank(this.currentRank); this.currentRank += 1; this.components.Add(box); }

    public void Open()
    {
        this.open = true;
        // increase the window size by contentSize (animate)
    }

    public void Close()
    {
        this.open = false;
        // decrease the window size by contentSize (animate)
    }

    public void Activate(bool state=true)
    {
        activeToggle.isOn = state;
        foreach (FishBox box in components) { box.Activate(state); }
        // grey out gameobject so toggle is still selectable and components interactable
    }

    public void ActivateTags(bool state=true) { foreach (FishBox box in components) { box.fish.ActivateUtil("tag", state); } }
    public void ActivateDepthLines(bool state=true) { foreach (FishBox box in components) { box.fish.ActivateUtil("line", state); } }
    public void ActivateTrails(bool state=true) { foreach (FishBox box in components) { box.fish.ActivateUtil("trail", state); } }
    public void ActivateThermos(bool state=true) { foreach (FishBox box in components) { box.fish.ActivateUtil("thermo", state); } }
}
