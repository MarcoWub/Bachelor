using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; //CLEANER
        texture.wrapMode = TextureWrapMode.Clamp; //Texture nicht wiederholen
        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }

    public static Texture2D TextureFromHeightMap(HeightMap heightMap)
    {
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        Color[] colorMap = new Color[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                colorMap[x + y * width] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue,heightMap.values[x, y]));
            }
        return TextureFromColorMap(colorMap, width, height);

    }

    public static Texture2D TextureFromTextureData(HeightMap heightMap, TextureData textureData, int width, int height)
    {

        Texture2D texture = new Texture2D(width, height);

        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                colorMap[x + y * width] = textureData.layers[0].tint;
                for (int i = 0; i < textureData.layers.Length; i++)
                    if (heightMap.values[x, y]/heightMap.maxValue >= textureData.layers[i].startHeight)
                        colorMap[x + y * width] = textureData.layers[i].tint;
            }
        return TextureFromColorMap(colorMap, width, height);
    }
}
