using Godot;
using System;

[GlobalClass]
public partial class MapDisplay : Node3D {

  [Export] public MeshInstance3D meshInstance {get; set;}
  [Export] public bool showNormals {get; set;} = true;
  [Export] public float normalLength {get; set;} = 1.0f;

  private MeshInstance3D normalVisualizationMesh;

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

    MapGenerator mapGeneratorRef =  GetNode<MapGenerator>("/root/Root/MapGenerator");
    meshInstance.Mesh = meshData.CreateMesh();
    meshInstance.Scale = new Vector3(
      mapGeneratorRef.MeshSettings.MeshScale,
      1.0f,
      mapGeneratorRef.MeshSettings.MeshScale
    );
    meshInstance.SetSurfaceOverrideMaterial(0, mapGeneratorRef.TerrainMaterial);
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
