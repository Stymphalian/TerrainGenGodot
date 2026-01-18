using Godot;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;


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
  public int chunkSizeIndex;
  [Export] int ChunkSizeIndex {
    get => chunkSizeIndex;
    set {
      chunkSizeIndex = Math.Clamp(value, 0, MeshGenerator.supportedChunkSizes.Length - 1);
      falloffMap = null;
      DrawMapInEditor();
    }
  }
  // public int mapChunkSize = 239;

  private int levelOfDetail = 0;
  private NoiseData noiseData;
  private TerrainData terrainData;
  private TextureData textureData;
  private Material terrainMaterial;
  private DRAW_MODE drawMode = DRAW_MODE.NOISE_MAP;
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

  [Export]
  public int LevelOfDetail {
    get => levelOfDetail;
    set {
      levelOfDetail = Math.Clamp(value, 0, MeshGenerator.numSupportedLODs - 1);
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
      GD.Print("Setting TextureData");
      textureData = value;
      textureData.ApplyToMaterial(terrainMaterial);  
      DrawMapInEditor();
      textureData.Changed += () => {
        DrawMapInEditor();
        textureData.ApplyToMaterial(terrainMaterial);
      };
      
    }
  }

  [Export]
  public Material TerrainMaterial {
    get => terrainMaterial;
    set {
      terrainMaterial = value;
      textureData.ApplyToMaterial(terrainMaterial);
      DrawMapInEditor();
    }
  }

  public int mapChunkSize {
    get => MeshGenerator.supportedChunkSizes[chunkSizeIndex];
  }

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

  public MapData GenerateMapData(Vector2 topLeft) {
    FastNoiseLite noise = NoiseGenerator.CreateCustom(
      noiseData.noiseSeed, noiseData.noiseFrequency, noiseData.noiseOctaves,
      noiseData.noiseLacunarity, noiseData.noisePersistence
    );

    float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(
      noise,
      mapChunkSize + 2,
      mapChunkSize + 2,
      noiseData.noiseScale,
      topLeft + noiseData.noiseOffset);

    if (terrainData.UseFalloffMap) {
      Debug.Assert(FalloffMap.GetLength(0) == noiseMap.GetLength(0), "Falloff map width does not match noise map width");
      Debug.Assert(FalloffMap.GetLength(1) == noiseMap.GetLength(1), "Falloff map height does not match noise map height");
      for (int y = 0; y < noiseMap.GetLength(1); y++) {
        for (int x = 0; x < noiseMap.GetLength(0); x++) {
          noiseMap[x, y] = Mathf.Clamp(noiseMap[x, y] - FalloffMap[x, y], 0, 1);
        }
      }
    }

    textureData.UpdateMeshHeights(
      (ShaderMaterial)terrainMaterial,
      terrainData.MinHeight,
      terrainData.MaxHeight
    );
    GD.Print("Min Height: " + terrainData.MinHeight, " Max Height: " + terrainData.MaxHeight);

    return new MapData(noiseMap);
  }

  public void DrawMapInEditor() {
    if (display == null) {
      GD.PrintErr("MapDisplay node not found!");
      return;
    }
    MapData mapData = GenerateMapData(Vector2.Zero);

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
      display.DrawMesh(meshData);
    } else if (drawMode == DRAW_MODE.FALLOFF_MAP) {
      Texture2D texture = TextureGenerator.TextureFromHeightMap(FalloffMap);
      display.DrawTexture(texture);
    } else if (drawMode == DRAW_MODE.NORMAL_MAP) {
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(
        mapData.HeightMap,
        terrainData.MeshHeightMultiplier,
        terrainData.HeightCurve,
        levelOfDetail,
        terrainData.UseFlatShading);

      Color[,] normalColorMap = new Color[meshData.meshLength, meshData.meshLength];
      for (int y = 0; y < meshData.meshLength; y++) {
        for (int x = 0; x < meshData.meshLength; x++) {
          Vector3 normal = meshData.Normals[x + y * meshData.meshLength];
          normalColorMap[x, y] = new Color(
            (normal.X + 1.0f) / 2.0f,
            (normal.Y + 1.0f) / 2.0f,
            (normal.Z + 1.0f) / 2.0f
          );
        }
      }
      Texture2D texture = TextureGenerator.TextureFromColorMap(normalColorMap);
      display.DrawTexture(texture);
    }
  }
}

