using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class PossionDiskData : UpdateableData
{
    public TextureData textureData;
    public List<PossionDiskLayer> layer;

    public void updateLayerFromData()
    {
        layer = new List<PossionDiskLayer>();
        foreach (TextureData.Layer lay in textureData.layers)
        {
            layer.Add(new PossionDiskLayer(lay.texture.name, 1f, lay.startHeight, new List<GameObject>()));
        }
    }


}

[System.Serializable]
public struct PossionDiskLayer
{
    public string name;
    
    [Range(0,1f)]
    public float multiplier;

    public bool noObjects;

    public float startHeight;

    public List<GameObject> objectsToPlace;

    public PossionDiskLayer(string name, float multiplier, float startHeight, List<GameObject> objectsToPlace)
    {
        this.name = name;
        this.multiplier = multiplier;
        this.startHeight = startHeight;
        this.objectsToPlace = objectsToPlace;
        noObjects = false;
    }
}
