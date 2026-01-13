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
    // COLOR_MAP,
    MESH,
    FALLOFF_MAP,
    NORMAL_MAP,
  };

  // public const int mapChunkSize = 241;
  public const int mapChunkSize = 239;
  private int levelOfDetail = 0;
  private NoiseData noiseData;
  private TerrainData terrainData;
  private TextureData textureData;
  private StandardMaterial3D terrainMaterial;

  private DRAW_MODE drawMode = DRAW_MODE.NOISE_MAP;

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

  [Export(PropertyHint.Range, "0,6")]
  public int LevelOfDetail {
    get => levelOfDetail;
    set {
      levelOfDetail = value;
      DrawMapInEditor();
    }
  }

  [Export]
  public NoiseData NoiseData {
    get => noiseData;
    set {
      noiseData = value;
      noiseData.Changed += DrawMapInEditor;
      DrawMapInEditor();
    }
  }

  [Export]
  public TerrainData TerrainData {
    get => terrainData;
    set {
      terrainData = value;
      terrainData.Changed += DrawMapInEditor;
      DrawMapInEditor();
    }
  }

  [Export]
  public TextureData TextureData {
    get => textureData;
    set {
      textureData = value;
      textureData.ApplyToMaterial(terrainMaterial);
      textureData.Changed += DrawMapInEditor;
      DrawMapInEditor();
    }
  }

  [Export]
  public StandardMaterial3D TerrainMaterial {
    get => terrainMaterial;
    set {
      terrainMaterial = value;
      textureData.ApplyToMaterial(terrainMaterial);
      DrawMapInEditor();
    }
  }

  // [Export]
  // public TerrainType[] Regions {
  //   get => regions;
  //   set {
  //     regions = value;
  //     DrawMapInEditor();
  //   }
  // }

  public float[,] FalloffMap {
    get {
      if (falloffMap == null) {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
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
        mapData.HeightMap,
        terrainData.MeshHeightMultiplier,
        terrainData.HeightCurve,
        lod,
        terrainData.UseFlatShading);
      lock (meshDataThreadQueue) {
        meshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
      }
    });
    meshDataThread.Start();
  }

  public Color[,] GetColorMapFromHeightMap(float[,] heightMap) {
    int width = heightMap.GetLength(0);
    int height = heightMap.GetLength(1);
    Color[,] colorMap = new Color[width, height];
    for (int y = 0; y < height; y++) {
      for (int x = 0; x < width; x++) {
        float currentHeight = heightMap[x, y];
        foreach (var region in regions) {
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
      noiseData.noiseSeed, noiseData.noiseFrequency, noiseData.noiseOctaves,
      noiseData.noiseLacunarity, noiseData.noisePersistence
    );

    int borderChunkSize = mapChunkSize + 2;
    float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(
      noise,
      borderChunkSize,
      borderChunkSize,
      noiseData.noiseScale,
      topLeft + noiseData.noiseOffset);
    if (terrainData.UseFalloffMap) {
      var localFalloffMap = FalloffMap;
      for (int y = 0; y < borderChunkSize; y++) {
        for (int x = 0; x < borderChunkSize; x++) {
          noiseMap[x, y] = Mathf.Clamp(noiseMap[x, y] - localFalloffMap[x, y], 0, 1);
        }
      }
    }
    // Color[,] colorMap = GetColorMapFromHeightMap(noiseMap);

    return new MapData(noiseMap);
  }

  public void DrawMapInEditor() {
    if (display == null) {
      GD.PrintErr("MapDisplay node not found!");
      return;
    }
    MapData mapData = GenerateMapData(Vector2.Zero);

    // if (drawMode == DRAW_MODE.COLOR_MAP) {
    //   Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.ColorMap);
    //   display.DrawTexture(texture);
    // } else 
    if (drawMode == DRAW_MODE.NOISE_MAP) {
      Texture2D texture = TextureGenerator.TextureFromHeightMap(mapData.HeightMap);
      display.DrawTexture(texture);
    } else if (drawMode == DRAW_MODE.MESH) {
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(
        mapData.HeightMap,
        terrainData.MeshHeightMultiplier,
        terrainData.HeightCurve,
        levelOfDetail,
        terrainData.UseFlatShading);
      // Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.ColorMap);
      display.DrawMesh(meshData);
    } else if (drawMode == DRAW_MODE.FALLOFF_MAP) {
      float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize);
      Texture2D texture = TextureGenerator.TextureFromHeightMap(falloffMap);
      display.DrawTexture(texture);
    } else if (drawMode == DRAW_MODE.NORMAL_MAP) {
      // MeshData meshData = MeshGenerator.GenerateSphereMesh(10.0f, 64, 64);
      // MeshData meshData = MeshGenerator.GenerateSphereMesh(mapData.HeightMap, 64, 64);
      // Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.ColorMap);
      // display.DrawMesh(meshData, texture);

      MeshData meshData = MeshGenerator.GenerateTerrainMesh(
        mapData.HeightMap,
        terrainData.MeshHeightMultiplier,
        terrainData.HeightCurve,
        levelOfDetail,
        terrainData.UseFlatShading);

      Color[,] normalColorMap = new Color[mapChunkSize, mapChunkSize];
      for (int y = 0; y < mapChunkSize; y++) {
        for (int x = 0; x < mapChunkSize; x++) {
          Vector3 normal = meshData.Normals[x + y * mapChunkSize];
          normalColorMap[x, y] = new Color(
            (normal.X + 1.0f) / 2.0f,
            (normal.Y + 1.0f) / 2.0f,
            (normal.Z + 1.0f) / 2.0f
          );
        }
      }
      // Texture2D texture = TextureGenerator.TextureFromColorMap(normalColorMap);
      display.DrawMesh(meshData);
    }
  }
}

