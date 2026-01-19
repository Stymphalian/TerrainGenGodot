using Godot;
using System;
using System.Diagnostics;

[GlobalClass]
public partial class MapPreview : Node3D {

  [Export] public MeshInstance3D meshInstance {get; set;}
  [Export] public bool showNormals {get; set;} = true;
  [Export] public float normalLength {get; set;} = 1.0f;
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
      textureData.ApplyToMaterial(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);  
      DrawMapInEditor();
      textureData.Changed += () => {
        DrawMapInEditor();
        textureData.ApplyToMaterial(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
      };
      
    }
  }

  [Export]
  public Material TerrainMaterial {
    get => terrainMaterial;
    set {
      terrainMaterial = value;
      textureData.ApplyToMaterial(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
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

  private MeshInstance3D normalVisualizationMesh;

  public override void _Ready() {
    DrawMapInEditor();
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

    return mapData;
  }

  public void DrawMapInEditor() {
    if (heightMapSettings == null || meshSettings == null) {
      return;
    }
    MapData heightMapData = GenerateMapData(Vector2.Zero);

    if (drawMode == DRAW_MODE.NOISE_MAP) {
      Texture2D texture = TextureGenerator.TextureFromHeightMap(heightMapData);
      DrawTexture(texture);
    } else if (drawMode == DRAW_MODE.MESH) {
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(
        heightMapData.HeightMap,
        meshSettings,
        editorLevelOfDetail);
      DrawMesh(meshData);
    } else if (drawMode == DRAW_MODE.FALLOFF_MAP) {
      Texture2D texture = TextureGenerator.TextureFromHeightMap(new MapData(0.0f, 1.0f, FalloffMap));
      DrawTexture(texture);
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
      DrawTexture(texture);
    }
  }

  public void DrawTexture(Texture2D texture) {
    // Find the MeshInstance child
    if (meshInstance == null) {
      GD.PrintErr("MeshInstance3D node not found!");
      return;
    }
    // meshInstance.Mesh = new PlaneMesh();
    // meshInstance.Scale = Vector3.One * 100 * GetNode<MapGenerator>("/root/Root/MapGenerator").TerrainData.TerrainUniformScale;
    meshInstance.SetSurfaceOverrideMaterial(0, new StandardMaterial3D {
      AlbedoTexture = texture,
      TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
      TextureRepeat = false,
      ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel, // Changed from PerVertex to PerPixel for proper lighting
    });
  }

  // public void DrawMesh(MeshData meshData, Texture2D texture) {
  public void DrawMesh(MeshData meshData) {
    if (meshInstance == null) {
      GD.PrintErr("MeshInstance3D node not found!");
      return;
    }

    // MapGenerator mapGeneratorRef =  GetNode<MapGenerator>("/root/Root/MapGenerator");
    meshInstance.Mesh = meshData.CreateMesh();
    meshInstance.Scale = new Vector3(
      meshSettings.MeshScale,
      1.0f,
      meshSettings.MeshScale
    );
    meshInstance.SetSurfaceOverrideMaterial(0, terrainMaterial);
    // if (material != null) {
    //   meshInstance.SetSurfaceOverrideMaterial(0, new StandardMaterial3D {
    //     // AlbedoTexture = texture,
    //     TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
    //     TextureRepeat = false,
    //     ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel, // Changed from PerVertex to PerPixel for proper lighting
    //   });  
    // }

    // Draw normal visualization if enabled
    if (showNormals) {
      if (normalVisualizationMesh == null) {
        normalVisualizationMesh = new MeshInstance3D();
        normalVisualizationMesh.Name = "NormalVisualization";
        AddChild(normalVisualizationMesh);
      }

      normalVisualizationMesh.Mesh = meshData.CreateNormalVisualizationMesh(normalLength);

      // Load and apply the normal visualization shader
      var shader = GD.Load<Shader>("res://NormalVisualization.gdshader");
      var shaderMaterial = new ShaderMaterial();
      shaderMaterial.Shader = shader;
      shaderMaterial.SetShaderParameter("normal_color", new Color(0, 1, 1, 1)); // Cyan color

      normalVisualizationMesh.SetSurfaceOverrideMaterial(0, shaderMaterial);
    } else if (normalVisualizationMesh != null) {
      normalVisualizationMesh.QueueFree();
      normalVisualizationMesh = null;
    }
  }

}
