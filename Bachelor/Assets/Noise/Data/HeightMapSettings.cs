using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class HeightMapSettings : UpdateableData
{
    [Tooltip("Skalierungsfaktor der Höhe")]
    public float heightMultiplier;

    [Tooltip("Einfluss des MeshHeightMultiplier auf die Höhe")]
    public AnimationCurve heightCurve;

    [Tooltip("Generiert Inseln")]
    public bool UseFalloff;


    [Tooltip("Soll der erste Layer flach sein? (Für Wasser)")]
    public bool firstLayerFlat;

    public float minHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(0);
        }
    }

    public float maxHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(1);
        }
    }

    public NoiseSettings noiseSettings;


    #if UNITY_EDITOR
    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }

    #endif

}
