using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public GameObject test;
    public GameObject treeContainer;
    public Pool pool;

    public enum DrawMode { NoiseMap, Mesh, FalloffMap, ColorMap };


    [Tooltip("Art des Modus")]
    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;
    public PossionDiskData possionDiskData;

    public Material terrainMaterial;


    //public const int mapChunkSize = 95; //239

    // 1, 2, 4, 6, 8, 10 ,12
    [Range(0, MeshSettings.numSupportedLODs - 1), Tooltip("Level of Detail, Anzahl der Vertices verringert sich")]
    public int editorPreviewLOD;

    [Tooltip("Automatische Updaten bei Veränderung von Werten")]
    public bool autoUpdate;

    public void DrawTexture(Texture2D texture)
    {

        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) /10f;

        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }

    public void DrawMapInEditor()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);

        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, Vector2.zero);

        if (treeContainer != null)
            DestroyImmediate(treeContainer);

        treeContainer = new GameObject("TreeContainer");
        treeContainer.AddComponent<HideOnPlay>();
        //treeContainer.transform.parent = this.transform;

        if (drawMode == DrawMode.NoiseMap)
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        else if (drawMode == DrawMode.ColorMap)
            DrawTexture(TextureGenerator.TextureFromTextureData(heightMap, textureData, heightMap.values.GetLength(0), heightMap.values.GetLength(1)));

        else if (drawMode == DrawMode.Mesh)
        {
            pool = new Pool(1000, test, treeContainer.transform);
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
            List<PoissonDiscSampling.Point> points = PoissonDiscSampling.GeneratePoints(heightMap, 1f, 20f, new Vector2(meshSettings.meshWorldSize, meshSettings.meshWorldSize), possionDiskData);
            Debug.Log(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                GameObject g = pool.RequestItem();
                float xPosition = points[i].position.x - 0.5f * meshSettings.meshWorldSize;
                float yPosition = points[i].position.y - 0.5f * meshSettings.meshWorldSize;
                g.transform.position = new Vector3(xPosition, heightMap.values[(int)(points[i].position.x), (int)(points[i].position.y)], -yPosition);
            }
        }
        else if (drawMode == DrawMode.FalloffMap)
            DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.numVertsPerLine), 0, 1)));
    }

    void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureData != null)
        {
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }

        if(possionDiskData != null)
        {
            possionDiskData.OnValuesUpdated -= DrawMapInEditor;
            possionDiskData.OnValuesUpdated += DrawMapInEditor;
        }
    }
}