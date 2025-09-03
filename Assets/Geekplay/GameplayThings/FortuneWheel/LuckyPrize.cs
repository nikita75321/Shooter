using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class LuckyPrize
{
    public string name;
    [Range(0, 1)] public float weight;
    public UnityAction reward;
}
