using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noise
{
    public enum NormalizeMode { Local, Global }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];


        //Jede Octave hat ein "zufälliges" Offset (Für Seed)
        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        float maxPossibleHeight = 0;

        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = prng.Next(-10000, 10000) + settings.offset.x + sampleCentre.x;
            float offsetY = prng.Next(-10000, 10000) - settings.offset.y - sampleCentre.y; //offset.y und offset.x für eigenen Offset zum durchscrollen!

            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= settings.persistance;
        }

        float maxNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;


        //Damit NoiseScale nicht oben recht sondern mittig zoomed!
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for(int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;
                for(int i = 0; i < settings.octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.scale * frequency; //Offset addieren für Seed, halfWidth für noiseScale zentral
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.scale * frequency; //Offset addieren für Seed, halfHeight für noiseScale zentral

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // AUCH NEGATIVE WERTE -> Interessanter

                    noiseHeight += perlinValue * amplitude;

                    amplitude *= settings.persistance;
                    frequency *= settings.lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                    maxNoiseHeight = noiseHeight;
                if (noiseHeight < minLocalNoiseHeight)
                    minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;

                if(settings.normalizeMode == NormalizeMode.Global)
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 2f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }               
            }

        if (settings.normalizeMode == NormalizeMode.Local)
        {
            for (int y = 0; y < mapHeight; y++)
                for (int x = 0; x < mapWidth; x++)
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
        }
        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSettings
{
    [Tooltip("Zoomfactor der Map")]
    public float scale = 50;

    [Tooltip("Anzahl der überlagerten Noises")]
    public int octaves = 6;
    [Tooltip("Abnahme der Amplitude der Octaven")]
    [Range(0, 1)]
    public float persistance =.6f;
    [Tooltip("Steigung der Frequenz der Octaven")]
    public float lacunarity = 2;

    [Tooltip("Seed der Map")]
    public int seed;
    [Tooltip("Eigenes Offset (Zum durchscrollen durch die Noise)")]
    public Vector2 offset;

    [Tooltip("Methode der Normalisierung, für endless Terrain GLOBAL")]
    public Noise.NormalizeMode normalizeMode;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1);
        persistance = Mathf.Clamp01(persistance);
    }
}
