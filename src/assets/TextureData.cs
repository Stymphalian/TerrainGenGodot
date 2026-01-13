using Godot;

[GlobalClass]
public partial class TextureData : Resource {

  public void ApplyToMaterial(StandardMaterial3D material) {
    // material.AlbedoTexture = AlbedoTexture;
    material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
    material.TextureRepeat = false;
  }
};