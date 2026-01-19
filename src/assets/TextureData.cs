using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class TextureData : Resource {

  private int textureSize = 512;
  private Image.Format textureFormat = Image.Format.Rgba8;
  private TextureLayer[] layers;

  [Export] public TextureLayer[] Layers {
    get => layers;
    set {
      layers = value;
      foreach(var layer in layers) {
        // layer.Changed -= EmitChanged;
        layer.Changed += EmitChanged;
      }
      EmitChanged();
    }
  }
  [Export] public int TextureSize {
    get => textureSize;
    set {
      textureSize = value;
      EmitChanged();
    }
  }
  [Export] public Image.Format TextureFormat {
    get => textureFormat;
    set {
      textureFormat = value;
      EmitChanged();
    }
  }

  public void ApplyToMaterial(Material material, float minHeight, float maxHeight) {
    if (material is ShaderMaterial) {
      var shaderMaterial = material as ShaderMaterial;
      shaderMaterial.SetShaderParameter("layerCount", layers.Length);
      shaderMaterial.SetShaderParameter("baseColors", layers.Select(x => x.Tint).ToArray());
      shaderMaterial.SetShaderParameter("baseStartHeights", layers.Select(x => x.StartHeight).ToArray());  
      shaderMaterial.SetShaderParameter("baseBlends", layers.Select(x => x.BlendStrength).ToArray());
      shaderMaterial.SetShaderParameter("baseColorStrengths", layers.Select(x => x.TintStrength).ToArray());
      shaderMaterial.SetShaderParameter("baseTextureScales", layers.Select(x => x.TextureScale).ToArray());
      shaderMaterial.SetShaderParameter("minHeight", minHeight);
      shaderMaterial.SetShaderParameter("maxHeight", maxHeight);
      
      Texture2DArray textureArray = GenerateTextureArray(layers.Select(x => x.Texture).ToList());
      shaderMaterial.SetShaderParameter("baseTextures", textureArray);
    }
  }

  private Texture2DArray GenerateTextureArray(List<Texture2D> textures) {
    Godot.Collections.Array<Image> images = new Godot.Collections.Array<Image>();
    for(int i = 0; i < textures.Count; i++) {
      var image = textures[i].GetImage();
      image.Resize(textureSize, textureSize);
      image.Convert(textureFormat);
      image.GenerateMipmaps();
      images.Add(image);
       GD.Print($"Added layer with size {image.GetSize()} and format {image.GetFormat()}");
    }
    Texture2DArray textureArray = new Texture2DArray();
    textureArray.CreateFromImages(images);
    return textureArray;
  }
};