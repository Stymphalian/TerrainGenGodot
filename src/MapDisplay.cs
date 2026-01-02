using Godot;
using System;

[GlobalClass]
public partial class MapDisplay : Node3D {

  [Export] public MeshInstance3D meshInstance {get; set;}

  public void DrawTexture(Texture2D texture) {
    // Find the MeshInstance child
    if (meshInstance == null) {
      GD.PrintErr("MeshInstance3D node not found!");
      return;
    }
    meshInstance.SetSurfaceOverrideMaterial(0, new StandardMaterial3D {
      AlbedoTexture = texture,
      TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
      TextureRepeat = false,
    });
  }

  public void DrawMesh(MeshData meshData, Texture2D texture) {
    if (meshInstance == null) {
      GD.PrintErr("MeshInstance3D node not found!");
      return;
    }

    meshInstance.Mesh = meshData.CreateMesh();
    meshInstance.SetSurfaceOverrideMaterial(0, new StandardMaterial3D {
      AlbedoTexture = texture,
      TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
      TextureRepeat = false,
    });
  }

}
