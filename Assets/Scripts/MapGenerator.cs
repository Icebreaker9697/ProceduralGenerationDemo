using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap, Mesh};
    public DrawMode drawMode;

    //This is specifying how much of the map we are generating at a time, and replaces mapWidth and mapHeight
    public const int mapChunkSize = 241;

    //We clamp this to the range 0 to 6 so that we can get the vertex increment by simply multiplying by 2
    //so that increments can be 2, 4, 6, 8, 10, 12. These are all useful, because (mapChunkSize - 1) = 240 which is divisible by all of the increments
    [Range(0,6)]
    public int levelOfDetail;

    public float noiseScale;
    public int octaves;

    //Increasing value controls how much smaller in height the features are with each octave
    //i.e. how much affect features have on the overall map
    [Range(0,1)]
    public float persistence;

    //Increasing value controls how many more smaller features show up with each octave
    public float lacunarity;

    //A seed so that we can get the same map if we specify a given seed
    public int seed;

    public Vector2 offset;

    public float meshHeightMultiplier;

    //used to make all the water flat, by specifying a range of heights to be flat
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public bool showOneOctave;

    public bool sampleDifferentPlacesForEachOctave;

    public int octaveToShow;

    public TerrainType[] regions;

    //used to hold the callbacks that need to be called on the main thread
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();
        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColorMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
        }
    }

    //Action<MapData> specifies that the type of callback is a function that takes a parameter of type MapData
    public void RequestMapData(Action<MapData> callback)
    {
        //This reperesents the Thread which runs MapData with the callback parameter
        ThreadStart threadStart = delegate {
            MapDataThread(callback);
        };

        //Start the thread
        new Thread(threadStart).Start();
    }

    public void RequestMeshData(MapData mapData, Action<MeshData> callback)
    {
        //This reperesents the Thread which runs MapData with the callback parameter
        ThreadStart threadStart = delegate {
            MeshDataThread(mapData, callback);
        };

        //Start the thread
        new Thread(threadStart).Start();
    }

    //This method runs on its own thread
    void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();
        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    void MeshDataThread(MapData mapData, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    //calls all the callbacks on the main thread
    void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if(meshDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    MapData GenerateMapData()
    {
        if(octaveToShow >= octaves)
        {
            octaveToShow = octaves - 1;
        }
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, offset, showOneOctave, octaveToShow, sampleDifferentPlacesForEachOctave);

        //assign a color to each coordinate
        Color[] colorMap = new Color[mapChunkSize * mapChunkSize];
        for(int y = 0; y < mapChunkSize; y++)
        {
            for(int x = 0; x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for(int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapChunkSize + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    private void OnValidate()
    {
        if(lacunarity < 1)
        {
            lacunarity = 1;
        }
        if(octaves < 0)
        {
            octaves = 0;
        }

        if(octaveToShow > octaves)
        {
            octaveToShow = octaves - 1;
        }

        if(octaveToShow < 0)
        {
            octaveToShow = 0;
        }
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
