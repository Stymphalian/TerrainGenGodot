using Godot;
using System;
using System.Threading;
using System.Collections.Generic;


public class MapThreadInfo<T> {
  public readonly Action<T> Callback;
  public readonly T Data;

  public MapThreadInfo(Action<T> callback, T data) {
    Callback = callback;
    Data = data;
  }
};

// [Tool]
public partial class MapGenerator : Node {
  public enum DRAW_MODE {
    NOISE_MAP,
    COLOR_MAP,
    MESH,
    FALLOFF_MAP
  };

  public const int mapChunkSize = 241;
  private int levelOfDetail = 0;
  // private int mapWidth = 100;
  // private int mapHeight = 100;
  private Vector2 noiseOffset = new Vector2(0, 0);
  private int noiseSeed = 0;
  private float noiseScale = 1.0f;
  private int noiseOctaves = 4;
  private float noiseFrequency = 0.05f;
  private float noiseLacunarity = 2.0f;
  private float noisePersistence = 0.5f;
  private DRAW_MODE drawMode = DRAW_MODE.NOISE_MAP;
  private float heightMultiplier = 10.0f;
  private Curve heightCurve;
  private TerrainType[] regions = [
    new TerrainType { Name = "Water Deep", Color = new Color(0, 0, 0.75f), Height = 0.3f },
    new TerrainType { Name = "Water Shallow", Color = new Color(0, 0, 0.8f), Height = 0.4f },
    new TerrainType { Name = "Sand", Color = new Color(0.76f, 0.7f, 0.5f), Height = 0.45f },
    new TerrainType { Name = "Grass", Color = new Color(0.1f, 0.6f, 0.1f), Height = 0.55f },
    new TerrainType { Name = "Grass 2", Color = new Color(0.0f, 0.5f, 0.0f), Height = 0.6f },
    new TerrainType { Name = "Rock", Color = new Color(0.55f, 0.4f, 0.25f), Height = 0.7f },
    new TerrainType { Name = "Rock 2", Color = new Color(0.45f, 0.3f, 0.2f), Height = 0.8f },
    new TerrainType { Name = "Snow", Color = new Color(1.0f, 1.0f, 1.0f), Height = 1.0f }
  ];
  private float[,] falloffMap;
  private bool useFalloffMap = false;

  [Export] public MapDisplay display { get; set; }

  // Add an editor button to regenerate the map
  [Export]
  public bool RegenerateMapButton {
    get => false;
    set {
      DrawMapInEditor();
    }
  }

  [Export]
  public Viewport.DebugDrawEnum debugDraw {
    get => GetViewport().DebugDraw;
    set => GetViewport().DebugDraw = value;
  }

  [Export]
  public DRAW_MODE DrawMode {
    get => drawMode;
    set {
      drawMode = value;
      DrawMapInEditor();
    }
  }

  [Export]
  float MeshHeightMultiplier {
    get => heightMultiplier;
    set {
      heightMultiplier = value;
      DrawMapInEditor();
    }
  }

  [Export]
  Curve HeightCurve {
    get => heightCurve;
    set {
      heightCurve = value;
      DrawMapInEditor();
    }
  }

  // [Export]
  // public int MapChunkSize {
  //   get => mapChunkSize;
  //   set {
  //     mapChunkSize = value;
  //     GenerateMap();
  //   }
  // }

  [Export(PropertyHint.Range, "0,6")]
  public int LevelOfDetail {
    get => levelOfDetail;
    set {
      levelOfDetail = value;
      DrawMapInEditor();
    }
  }


  [Export(PropertyHint.Range, "0.0001,100.0,0.0001")]
  public float NoiseScale {
    get => noiseScale;
    set {
      noiseScale = value;
      DrawMapInEditor();
    }
  }

  [Export(PropertyHint.Range, "0.0001,100.0,0.0001")]
  public float NoiseFrequency {
    get => noiseFrequency;
    set {
      noiseFrequency = value;
      DrawMapInEditor();
    }
  }

  [Export(PropertyHint.Range, "1,16")]
  public int NoiseOctaves {
    get => noiseOctaves;
    set {
      noiseOctaves = value;
      DrawMapInEditor();
    }
  }

  [Export(PropertyHint.Range, "0.0001,100.0,0.0001")]
  // Lacunarity: frequency multiplier between octaves
  public float NoiseLacunarity {
    get => noiseLacunarity;
    set {
      noiseLacunarity = value;
      DrawMapInEditor();
    }
  }

  [Export(PropertyHint.Range, "0.0,1.0,0.001")]
  public float NoisePersistence {
    get => noisePersistence;
    set {
      noisePersistence = value;
      DrawMapInEditor();
    }
  }

  [Export]
  public Vector2 NoiseOffset {
    get => noiseOffset;
    set {
      noiseOffset = value;
      DrawMapInEditor();
    }
  }

  [Export]
  public int NoiseSeed {
    get => noiseSeed;
    set {
      noiseSeed = value;
      DrawMapInEditor();
    }
  }

  [Export]
  public TerrainType[] Regions {
    get => regions;
    set {
      regions = value;
      DrawMapInEditor();
    }
  }

  [Export]
  public bool UseFalloffMap {
    get => useFalloffMap;
    set {
      useFalloffMap = value;
      DrawMapInEditor();
    }
  }

  public float[,] FalloffMap  {
    get {
      if (falloffMap == null) {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
      }
      return falloffMap;
    }
  }

  public override void _Ready() {
    DrawMapInEditor();
  }

  private Queue<MapThreadInfo<MapData>> mapDataThreadQueue = new Queue<MapThreadInfo<MapData>>();
  private Queue<MapThreadInfo<MeshData>> meshDataThreadQueue = new Queue<MapThreadInfo<MeshData>>();

  public override void _Process(double delta) {
    base._Process(delta);
    if (mapDataThreadQueue.Count > 0) {
      for (int index = 0; index < mapDataThreadQueue.Count; index++) {
        var threadData = mapDataThreadQueue.Dequeue();
        threadData.Callback(threadData.Data);
      }
    }

    if (meshDataThreadQueue.Count > 0) {
      for (int index = 0; index < meshDataThreadQueue.Count; index++) {
        var threadData = meshDataThreadQueue.Dequeue();
        threadData.Callback(threadData.Data);
      }
    }
  }

  public void RequestMapData(Vector2 topLeft, Action<MapData> callback) {
    Thread mapDataThread = new Thread(() => {
      MapData mapData = GenerateMapData(topLeft);
      lock (mapDataThreadQueue) {
        mapDataThreadQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
      }
    });
    mapDataThread.Start();
  }

  public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
    Thread meshDataThread = new Thread(() => {
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(
        mapData.HeightMap, heightMultiplier, heightCurve, lod);
      lock (meshDataThreadQueue) {
        meshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
      }
    });
    meshDataThread.Start();
  }


  public Color[,] GetColorMapFromHeightMap(float[,] heightMap) {
    Color[,] colorMap = new Color[mapChunkSize, mapChunkSize];
    for (int y = 0; y < mapChunkSize; y++) {
      for (int x = 0; x < mapChunkSize; x++) {
        float currentHeight = heightMap[x, y];
        foreach (var region in Regions) {
          if (currentHeight <= region.Height) {
            colorMap[x, y] = region.Color;
            break;
          }
        }
      }
    }
    return colorMap;
  }

  public MapData GenerateMapData(Vector2 topLeft) {
    FastNoiseLite noise = NoiseGenerator.CreateCustom(
      noiseSeed, noiseFrequency, noiseOctaves,
      noiseLacunarity, noisePersistence
    );
    float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(
      noise, mapChunkSize, mapChunkSize, noiseScale, topLeft + noiseOffset);
    if (useFalloffMap) {
      var localFalloffMap = FalloffMap;
      for (int y = 0; y < mapChunkSize; y++) {
        for (int x = 0; x < mapChunkSize; x++) {
          noiseMap[x, y] = Mathf.Clamp(noiseMap[x, y] - localFalloffMap[x, y], 0, 1);
        }
      }
    }

    Color[,] colorMap = GetColorMapFromHeightMap(noiseMap);

    return new MapData(noiseMap, colorMap);
  }

  public void DrawMapInEditor() {
    if (display == null) {
      GD.PrintErr("MapDisplay node not found!");
      return;
    }
    MapData mapData = GenerateMapData(Vector2.Zero);

    if (drawMode == DRAW_MODE.COLOR_MAP) {
      Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.ColorMap);
      display.DrawTexture(texture);
    } else if (drawMode == DRAW_MODE.NOISE_MAP) {
      Texture2D texture = TextureGenerator.TextureFromHeightMap(mapData.HeightMap);
      display.DrawTexture(texture);
    } else if (drawMode == DRAW_MODE.MESH) {
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(
        mapData.HeightMap, heightMultiplier, heightCurve, levelOfDetail);
      Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.ColorMap);
      display.DrawMesh(meshData, texture);
    } else if (drawMode == DRAW_MODE.FALLOFF_MAP) {
      float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
      Texture2D texture = TextureGenerator.TextureFromHeightMap(falloffMap);
      display.DrawTexture(texture);
    }
  }
}

