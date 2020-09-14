using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PoissonDiscSampling
{
    public static float[,] heightMap;
    public struct Point
    {
        public Vector2 position;
        public float radius;

        public Point(Vector2 p, float r)
        {
            position = p;
            radius = r;
        }
    }
    public static List<Point> GeneratePoints(HeightMap heightMap, float minRadius, float maxRadius, Vector2 sampleRegionSize, PossionDiskData possionDiskData, int numSamplesBeforeRejection = 30)
    {

        float cellSize = maxRadius / Mathf.Sqrt(2);
        
        List<Point>[,] grid = new List<Point>[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];

        for (int i = 0; i < grid.GetLength(0); i++)
            for (int j = 0; j < grid.GetLength(1); j++)
                grid[i, j] = new List<Point>();

        List<Point> points = new List<Point>();
        List<Point> spawnPoints = new List<Point>();


        float height = heightMap.values[(int)((sampleRegionSize.x)), (int)((sampleRegionSize.y))];
        height = Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, height);
        int k = -1;
        for (; k < possionDiskData.layer.Count-1; k++)
        {
            if (height < possionDiskData.layer[k+1].startHeight)
                break;
        }
        float multiplier = possionDiskData.layer[k].multiplier;

        float radiusForNewPoint = multiplier * (maxRadius - minRadius) + minRadius;
        spawnPoints.Add(new Point(sampleRegionSize / 2, radiusForNewPoint));
        while (spawnPoints.Count > 0) 
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 spawnCentre = spawnPoints[spawnIndex].position;
            bool candidateAccepted = false;
            float radius = spawnPoints[spawnIndex].radius;
            for (int i = 0; i < numSamplesBeforeRejection; i++)
            {
               
                float angle = Random.value * Mathf.PI * 2;
                Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                
                Vector2 newCenterPoint = spawnCentre + dir * Random.Range(radius, 2 * radius);
                float newRadius = maxRadius;
                try
                {
                    height = heightMap.values[(int)(newCenterPoint.x ), (int)(newCenterPoint.y )];
                    height = Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, height);

                    k = 0;
                    for (; k < possionDiskData.layer.Count; k++)
                    {
                        if (height < possionDiskData.layer[k].startHeight)
                            break;
                    }
                    if (possionDiskData.layer[k - 1].noObjects)
                        continue;
                    multiplier = possionDiskData.layer[k-1].multiplier; 
                    newRadius = multiplier * (maxRadius - minRadius) + minRadius;
                }
                catch
                {
                   
                }
                Point candidate = new Point(newCenterPoint, newRadius);
               
                if (IsValid(candidate, sampleRegionSize, cellSize, newRadius, points, grid))
                {                    
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.position.x / cellSize), (int)(candidate.position.y / cellSize)].Add(candidate);
                    candidateAccepted = true;
                    break;
                }
            }
            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }

        }

        return points;
    }

    static bool IsValid(Point candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Point> points, List<Point>[,] grid)
    {
        if (candidate.position.x >= 0 && candidate.position.x < sampleRegionSize.x && candidate.position.y >= 0 && candidate.position.y < sampleRegionSize.y)
        {
            int cellX = (int)(candidate.position.x / cellSize);
            int cellY = (int)(candidate.position.y / cellSize);


            int searchStartX = Mathf.Max(0, cellX - 2);
            int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
            int searchStartY = Mathf.Max(0, cellY - 2);
            int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int y = searchStartY; y <= searchEndY; y++)
                {
                    List<Point> pointIndex = grid[x, y];
                    if (pointIndex != null)
                    {
                        foreach(Point p in pointIndex)
                        {
                            float sqrDst = (candidate.position - p.position).sqrMagnitude;
                            if (sqrDst < p.radius * p.radius && sqrDst < candidate.radius * candidate.radius)
                            {
                                return false;
                            }
                        }                      
                    }
                }
            }
            return true;
        }
        return false;
    }
}