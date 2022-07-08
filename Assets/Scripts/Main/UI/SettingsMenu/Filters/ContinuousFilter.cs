public class ContinuousFilter : Filter
{
    public float maxVal{ get; private set;}
    public float minVal{ get; private set;}
    public bool inverted{ get; private set;}
    public bool length { get; private set;}
    public ContinuousFilterTile tile { get; private set;}


    public ContinuousFilter(float maxVal, float minVal, bool inverted, bool length)
    {
        this.maxVal = maxVal;
        this.minVal = minVal;
        this.inverted = inverted;
        this.length = length;

        this.tile = FiltersBar.instance.AddCont(this);
    }

    public override bool PassesFilter(Fish fish)
    {
        if (this.length)
        {
            if (fish.length == null) { return false; }
            else if (!this.inverted) { if ((float)fish.length < this.maxVal && (float)fish.length > this.minVal) { return true; } }
            else if (this.inverted) { if (!((float)fish.length < this.maxVal && (float)fish.length > this.minVal)) { return true; } }
        }
        else
        {
            if (fish.weight == null) { return false; }
            else if (!this.inverted) { if ((float)fish.weight < this.maxVal && (float)fish.weight > this.minVal) { return true; } }
            else if (this.inverted) { if (!((float)fish.weight < this.maxVal && (float)fish.weight > this.minVal)) { return true; } }
        }

        return false;
    }
}