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

  private int editorLevelOfDetail = 0;
  private HeightMapSettings heightMapSettings;
  private MeshSettings meshSettings;
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
  public int EditorLevelOfDetail {
    get => editorLevelOfDetail;
    set {
      editorLevelOfDetail = Math.Clamp(value, 0, MeshSettings.NumSupportedLODs - 1);
      DrawMapInEditor();
    }
  }

  [Export]
  public HeightMapSettings HeightMapSettings {
    get => heightMapSettings;
    set {
      heightMapSettings = value;
      heightMapSettings.Changed += DrawMapInEditor;
      DrawMapInEditor();
    }
  }

  [Export]
  public MeshSettings MeshSettings {
    get => meshSettings;
    set {
      meshSettings = value;
      meshSettings.Changed += DrawMapInEditor;
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

  private bool _visualizeLODWithMaterial = false;
  [Export] public bool VisualizeLODWithMaterial {
    get => _visualizeLODWithMaterial;
    set {
      _visualizeLODWithMaterial = value;
      DrawMapInEditor();
    }
  }

  public float[,] FalloffMap {
    get {
      if (falloffMap == null) {
        falloffMap = FalloffGenerator.GenerateFalloffMap(meshSettings.BorderedMeshSize);
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
      MapData heightMapData = GenerateMapData(topLeft);
      lock (mapDataThreadQueue) {
        mapDataThreadQueue.Enqueue(new MapThreadInfo<MapData>(callback, heightMapData));
      }
    });
    mapDataThread.Start();
  }

  public void RequestMeshData(MapData heightMapData, int lod, Action<MeshData> callback) {
    Thread meshDataThread = new Thread(() => {
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(
        heightMapData.HeightMap,
        meshSettings,
        lod);
      lock (meshDataThreadQueue) {
        meshDataThreadQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
      }
    });
    meshDataThread.Start();
  }

  public MapData GenerateMapData(Vector2 topLeft) {
    MapData mapData = HeightMapGenerator.GenerateHeightMap(
      meshSettings.BorderedMeshSize, 
      meshSettings.BorderedMeshSize,
      heightMapSettings,
      topLeft
    );

    // TODO: Should be done in the GenerateHeightMap() function
    if (heightMapSettings.UseFalloffMap) {
      Debug.Assert(FalloffMap.GetLength(0) == mapData.HeightMap.GetLength(0), "Falloff map width does not match noise map width");
      Debug.Assert(FalloffMap.GetLength(1) == mapData.HeightMap.GetLength(1), "Falloff map height does not match noise map height");
      for (int y = 0; y < mapData.HeightMap.GetLength(1); y++) {
        for (int x = 0; x < mapData.HeightMap.GetLength(0); x++) {
          float falloffValue = FalloffMap[x, y] * (mapData.MaxHeight - mapData.MinHeight);
          mapData.HeightMap[x, y] = Mathf.Clamp(
            mapData.HeightMap[x, y] - falloffValue,
            mapData.MinHeight,
            mapData.MaxHeight
          );
        }
      }
    }

    textureData.UpdateMeshHeights(
      (ShaderMaterial)terrainMaterial,
      heightMapSettings.MinHeight,
      heightMapSettings.MaxHeight
    );
    // GD.Print("Min Height: " + heightMapSettings.MinHeight, " Max Height: " + heightMapSettings.MaxHeight);
    // GD.Print("Min Height: " + mapData.MinHeight, " Max Height: " + mapData.MaxHeight);

    return mapData;
  }

  public void DrawMapInEditor() {
    if (display == null) {
      GD.PrintErr("MapDisplay node not found!");
      return;
    }
    MapData heightMapData = GenerateMapData(Vector2.Zero);

    if (drawMode == DRAW_MODE.NOISE_MAP) {
      Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMapData.HeightMap);
      display.DrawTexture(texture);
    } else if (drawMode == DRAW_MODE.MESH) {
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(
        heightMapData.HeightMap,
        meshSettings,
        editorLevelOfDetail);
      display.DrawMesh(meshData);
    } else if (drawMode == DRAW_MODE.FALLOFF_MAP) {
      Texture2D texture = TextureGenerator.TextureFromHeightMap(FalloffMap);
      display.DrawTexture(texture);
    } else if (drawMode == DRAW_MODE.NORMAL_MAP) {
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(
        heightMapData.HeightMap,
        meshSettings,
        editorLevelOfDetail);

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

