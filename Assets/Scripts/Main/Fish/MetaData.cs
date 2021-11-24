using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Data;
using TMPro;

public class MetaData
{
    public string fishSex, fishSpecies;
    public int age;
    public float depth, size;

    public MetaData(Fisch fish)
    {
        this.depth = fish.fishObject.transform.position.y;
    }
}