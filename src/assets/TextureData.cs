using Godot;

[GlobalClass]
public partial class TextureData : Resource {

  private Color[] baseColors = [
    new Color(0, 0, 0.8f), // Deep Water
    new Color(0.1f, 0.6f, 0.1f), // Grass
    new Color(0.55f, 0.4f, 0.25f),
    new Color(1.0f, 1.0f, 1.0f), // Snow
  ];
  private float[] baseStartHeights = [
    0.3f, // Deep Water
    0.55f, // Grass
    0.7f,
    0.9f, // Snow
  ];

  [Export] public Color[] BaseColors {
    get => baseColors;
    set {
      baseColors = value;
      EmitChanged();
    }
  }

  [Export] public float[] BaseStartHeights {
    get => baseStartHeights;
    set {
      baseStartHeights = value;
      EmitChanged();
    }
  }

  // private TerrainType[] regions = [
  //   new TerrainType { Name = "Water Deep", Color = new Color(0, 0, 0.75f), Height = 0.3f },
  //   new TerrainType { Name = "Water Shallow", Color = new Color(0, 0, 0.8f), Height = 0.4f },
  //   new TerrainType { Name = "Sand", Color = new Color(0.76f, 0.7f, 0.5f), Height = 0.45f },
  //   new TerrainType { Name = "Grass", Color = new Color(0.1f, 0.6f, 0.1f), Height = 0.55f },
  //   new TerrainType { Name = "Grass 2", Color = new Color(0.0f, 0.5f, 0.0f), Height = 0.6f },
  //   new TerrainType { Name = "Rock", Color = new Color(0.55f, 0.4f, 0.25f), Height = 0.7f },
  //   new TerrainType { Name = "Rock 2", Color = new Color(0.45f, 0.3f, 0.2f), Height = 0.8f },
  //   new TerrainType { Name = "Snow", Color = new Color(1.0f, 1.0f, 1.0f), Height = 1.0f }
  // ];

  public void ApplyToMaterial(Material material) {
    if (material is ShaderMaterial) {
      var shaderMaterial = material as ShaderMaterial;
      shaderMaterial.SetShaderParameter("baseColorCount", BaseColors.Length);
      shaderMaterial.SetShaderParameter("baseColors", BaseColors);
      shaderMaterial.SetShaderParameter("baseStartHeights", BaseStartHeights);  
    }
    
    // material.AlbedoTexture = AlbedoTexture;
    // material.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
    // material.TextureRepeat = false;
  }

  public void UpdateMeshHeights(ShaderMaterial material, float minHeight, float maxHeight) {
    material.SetShaderParameter("minHeight", minHeight);
    material.SetShaderParameter("maxHeight", maxHeight);
    // material.SetShaderParameter("minHeight", minHeight);
    // material.SetShaderParameter("maxHeight", maxHeight);
  }
};