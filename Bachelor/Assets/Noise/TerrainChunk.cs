using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunk : MonoBehaviour
{
    const float colliderGenerationDistanceThreshold = 10f;
    public event System.Action<TerrainChunk, bool> OnVisibiltyChanged;
    public Vector2 coord;

    GameObject meshObject;
    Vector2 sampleCenter;
    Bounds bounds;

    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;
    int colliderLODIndex;

    HeightMap heightMap;
    bool heightMapReceived;

    int previousLODIndex = -1;
    bool hasSetCollider;
    float maxViewDistance;

    List<PoissonDiscSampling.Point> points;
    List<List<GameObject>> treeList;

    HeightMapSettings heightMapSettings;
    MeshSettings meshSettings;
    PossionDiskData possionDiskData;
    Transform viewer;
    bool hasObjectPlaced = false;

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, PossionDiskData possionDiskData, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
    {
        this.possionDiskData = possionDiskData;
        this.coord = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderLODIndex;
        this.meshSettings = meshSettings;
        this.heightMapSettings = heightMapSettings;
        this.viewer = viewer;
        treeList = new List<List<GameObject>>();
        for (int i = 0; i < TerrainGenerator.treePools.Count; i++)
            treeList.Add(new List<GameObject>());

        sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.meshWorldSize;
        bounds = new Bounds(position, Vector2.one * meshSettings.meshWorldSize);



        meshObject = new GameObject("TerrainChunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        meshCollider = meshObject.AddComponent<MeshCollider>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshObject.transform.position = new Vector3(position.x, 0, position.y);
        meshObject.transform.parent = parent;

        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];

        for (int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
            {
                lodMeshes[i].updateCallback += UpdateCollisionMesh;
            }
        }

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDstThreshold;

        
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVertsPerLine, meshSettings.numVertsPerLine, heightMapSettings, sampleCenter), OnHeightMapReceived);
    }

    void OnHeightMapReceived(object heightMap)
    {
        this.heightMap = (HeightMap)heightMap;
        this.heightMapReceived = true;

        this.points = PoissonDiscSampling.GeneratePoints(this.heightMap, 1f, 20f, new Vector2(meshSettings.meshWorldSize, meshSettings.meshWorldSize), possionDiskData);

        UpdateTerrainChunk();
    }

    Vector2 viewerPosition
    {
        get
        {
            return new Vector2(viewer.position.x, viewer.position.z);
        }
    }


    public bool ShouldBeVisible()
    {
        float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
        return viewerDistanceFromNearestEdge <= maxViewDistance;
    }


    //Chunk updated sich selber:
    //Point finden der am nähsten zur Position des Spielers ist
    //Distanz ermitteln
    //WEnn Distanz größer als MAxViewDistance, dann hiden (disable)
    // Sonst enableen
    public void UpdateTerrainChunk()
    {
        if (!heightMapReceived)
            return;
        float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
        bool wasVisible = isVisible();
        bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

        int lodIndex = 0;
        if (visible)
        {
            for (int i = 0; i < detailLevels.Length - 1; i++)
            {
                if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDstThreshold)
                    lodIndex = i + 1;
                else
                    break;
            }
            if (lodIndex != previousLODIndex)
            {
                LODMesh lodMesh = lodMeshes[lodIndex];
                if (lodMesh.hasMesh)
                {
                    previousLODIndex = lodIndex;
                    meshFilter.mesh = lodMesh.mesh;                                   
                }
                else if (!lodMesh.hasRequestedMesh)
                {
                    lodMesh.RequestMesh(heightMap, meshSettings);
                }

                if (lodIndex == 0 && !hasObjectPlaced)
                {
                    hasObjectPlaced = true;
                    for (int i = 0; i < points.Count; i++)
                    {

                        

                        float height = heightMap.values[(int)(points[i].position.x), (int)(points[i].position.y)];
                        int layerCount;
                        PossionDiskLayer layer;

                        (layerCount, layer)  = GetLayerFromHeight(height);
                        int randomValue = Random.Range(0, layer.objectsToPlace.Count);
                        GameObject g = TerrainGenerator.treePools[layerCount][randomValue].RequestItem();
                        float xPosition = points[i].position.x - 0.5f * meshSettings.meshWorldSize;
                        float yPosition = points[i].position.y - 0.5f * meshSettings.meshWorldSize;
                        float randomRotation = Random.Range(0, Mathf.PI * 2);
                        if (g)
                        {
                            try
                            {
                                treeList[layerCount].Add(g);
                                g.transform.position = new Vector3(xPosition + sampleCenter.x, height, -yPosition + sampleCenter.y);
                                g.transform.rotation = new Quaternion(0, randomRotation, 0, 1);
                            }
                            catch
                            {
                                Debug.Log("RANDOMVALUE " + randomValue);
                                Debug.Log("TRELISTCOUNT " + treeList.Count);
                            }

                        }
                    }
                }                    
                if(lodIndex > 0 && hasObjectPlaced)
                {
                    hasObjectPlaced = false;
                    for (int i = 0; i < treeList.Count; i++)
                    {
                        for(int j = 0; j < treeList[i].Count; j++)
                        {
                            GameObject g = treeList[i][j];
                            float height = heightMap.values[(int)(points[i].position.x), (int)(points[i].position.y)];
                            int layerCount;
                            PossionDiskLayer layer;

                            (_, layer) = GetLayerFromHeight(height);
                            int randomValue = Random.Range(0, layer.objectsToPlace.Count);

                            TerrainGenerator.treePools[i][randomValue].ReturnItem(g);
                        }
                        treeList[i].Clear();
                    }
                }

            }

        }

        if (wasVisible != visible)
        {
            /*
            if(visible)
                if(lodIndex == 0 && !hasObjectPlaced)
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        int randomValue = Random.Range(0, 3);
                        GameObject g = TerrainGenerator.treePools[randomValue].RequestItem();
                        float xPosition = points[i].position.x - 0.5f * meshSettings.meshWorldSize;
                        float yPosition = points[i].position.y - 0.5f * meshSettings.meshWorldSize;
                        if (g)
                        {
                            treeList[randomValue].Add(g);
                            g.transform.position = new Vector3(xPosition + sampleCenter.x, heightMap.values[(int)(points[i].position.x), (int)(points[i].position.y)], -yPosition + sampleCenter.y);
                        }
                    }
                    hasObjectPlaced = true;
                }
                    

            if(wasVisible)
            {
                for(int i = 0; i < treeList.Count; i++)
                {
                    for (int j = 0; j < treeList[i].Count; j++)
                    {
                        GameObject g = treeList[i][j];
                        TerrainGenerator.treePools[i].ReturnItem(g);
                    }
                }
                treeList.Clear();
                hasObjectPlaced = false;
            }*/

            SetVisible(visible);
            if (OnVisibiltyChanged != null)
            {
                OnVisibiltyChanged(this, visible);
            }    
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!hasSetCollider)
        {
            float sqrDstFromViewerToEdge = bounds.SqrDistance(viewerPosition);
            if (sqrDstFromViewerToEdge < detailLevels[colliderLODIndex].sqrvisibleDstThreshold)
                if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
                    lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);

            if (sqrDstFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
            {
                if (lodMeshes[colliderLODIndex].hasMesh)
                {
                    meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
                    hasSetCollider = true;
                }

            }
        }
    }

    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }

    public bool isVisible()
    {
        return meshObject.activeSelf;
    }

    public (int,PossionDiskLayer) GetLayerFromHeight(float height)
    {
        int k = 0;
        for (; k < possionDiskData.layer.Count; k++)
        {
            if (height < possionDiskData.layer[k].startHeight)
                break;
        }
        return (k-2, possionDiskData.layer[k - 2]);
    }
}

class LODMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh; //ReceivedMesh
    int lod;
    public event System.Action updateCallback;
    public bool hasPoints;


    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    void OnMeshDataReceived(object meshData)
    {
        mesh = ((MeshData)meshData).CreateMesh();
        hasMesh = true;
        updateCallback();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataReceived);
    }
}

