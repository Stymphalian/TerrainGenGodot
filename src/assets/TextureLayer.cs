using Godot;

[GlobalClass]
public partial class TextureLayer : Resource {
  public Texture2D texture;
  public Color tint;
  public float tintStrength;
  public float startHeight;
  public float blendStrength;
  public float textureScale;

  [Export] public Texture2D Texture {
    get => texture;
    set {
      texture = value;
      EmitChanged();
    }
  }
  [Export] public Color Tint {
    get => tint;
    set {
      tint = value;
      EmitChanged();
    }
  }
  [Export] public float TintStrength {
    get => tintStrength;
    set {
      tintStrength = value;
    EmitChanged();  
    }
  }
  [Export] public float StartHeight {
    get => startHeight;
    set {
      startHeight = value;
      EmitChanged();
    }
  }
  [Export] public float BlendStrength {
    get => blendStrength;
    set {
      blendStrength = value;
      EmitChanged();
    }
  }
  [Export] public float TextureScale {
    get => textureScale;
    set {
      textureScale = value;
      EmitChanged();
    }
  }
};