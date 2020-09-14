using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TerrainGenerator : MonoBehaviour
{
   
    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate; //Square Distance Performance technisch BESSER


    public int colliderLODIndex;
    public LODInfo[] detailLevels;
    public Transform chunkCollection;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;
    public PossionDiskData possionDiskData;

    public Transform viewer;
    public Material mapMaterial;

    public static List<List<Pool>> treePools;

    public Vector2 viewerPosition; 
    Vector2 viewerPositionOld;

    float meshWorldSize; // Chunk Größe
    int chunksVisibleInViewDistance; //Sichtbare Chunks

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    int MaterialIsRight = 20;

    void Start()
    {
       

        float maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / meshWorldSize);

        if (treePools == null)
        {
            treePools = new List<List<Pool>>();
            for (int i = 0; i < possionDiskData.layer.Count; i++)
            {
                treePools.Add(new List<Pool>());
                for (int j = 0; j < possionDiskData.layer[i].objectsToPlace.Count; j++)
                {
                    treePools[i].Add(new Pool(2500, possionDiskData.layer[i].objectsToPlace[j], this.transform));
                }
            }
        }

        textureData.ApplyToMaterial(mapMaterial);
        textureData.UpdateMeshHeights(mapMaterial, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        UpdateVisibleChunks();
    }

    void Update()
    {
        if(MaterialIsRight > 0)
        {
            MaterialIsRight--;
            textureData.ApplyToMaterial(mapMaterial);
        }
            
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        
        if(viewerPosition != viewerPositionOld)
        {
            foreach(TerrainChunk chunk in visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate) // NUR DANN UPDATEN
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
            UpdateVisibleChunksForGarbageCollection();
        }

        

    }

    void UpdateVisibleChunksForGarbageCollection()
    {
        for(int i = 0; i < chunkCollection.childCount; i++)
        {
            GameObject terrainChunkGameObject = chunkCollection.GetChild(i).gameObject;
            if(terrainChunkGameObject.activeSelf)
            {
                Vector2 coords = new Vector2(terrainChunkGameObject.transform.position.x / 242f, terrainChunkGameObject.transform.position.z / 242f);
                TerrainChunk terrainChunk = terrainChunkDictionary[coords];
                if (!terrainChunk.ShouldBeVisible())
                {
                    terrainChunkGameObject.SetActive(false);
                }
            }         
        }
    }

    void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for(int i = visibleTerrainChunks.Count-1; i >= 0; i-- )
        {
            visibleTerrainChunks[i].UpdateTerrainChunk();
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
        }


        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);
        
        for(int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewerChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                if (!alreadyUpdatedChunkCoords.Contains(viewerChunkCoord))
                {               
                    if (terrainChunkDictionary.ContainsKey(viewerChunkCoord)) //schon vorhanden
                        terrainChunkDictionary[viewerChunkCoord].UpdateTerrainChunk();
                    else //Zum ersten mal generieren   
                    {
                        TerrainChunk terrainChunk = new TerrainChunk(viewerChunkCoord, heightMapSettings, meshSettings, possionDiskData, detailLevels, colliderLODIndex, chunkCollection, viewer, mapMaterial);
                        terrainChunkDictionary.Add(viewerChunkCoord, terrainChunk);
                        terrainChunk.OnVisibiltyChanged += OnTerrainChunkVisibiltyChanged;
                        terrainChunk.Load();
                    }
                       
                }
            }
    }
    
    void OnTerrainChunkVisibiltyChanged(TerrainChunk chunk, bool isVisible)
    {
        if(isVisible)
        {
            visibleTerrainChunks.Add(chunk);
        }
        else
        {
            visibleTerrainChunks.Remove(chunk);
        }
    }
}


[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float visibleDstThreshold; //Distance in dem der LOD zählt, wenn drüber switch zu anderem LOD

    public float sqrvisibleDstThreshold
    {
        get
        {
            return visibleDstThreshold * visibleDstThreshold;
        }
    }
}
