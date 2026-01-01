using Godot;
using System;

[Tool]
public partial class TexturePreview : Node2D {

  private Sprite2D _sprite;
  private FastNoiseLite _noise = NoiseGenerator.Create();

  private int _textureWidth = 256;
  private int _textureHeight = 256;
  private int _noiseSeed = 0;
  private float _noiseFrequency = 0.01f;
  private int _noiseOctaves = 4;
  private float _noiseLacunarity = 2.0f;
  private float _noiseGain = 0.5f;
  private Vector2 _textureOffset = new Vector2(0, 0);

  [Export]
  public int TextureWidth {
    get => _textureWidth;
    set {
      _textureWidth = value;
      UpdateTexture();
    }
  }

  [Export]
  public int TextureHeight {
    get => _textureHeight;
    set {
      _textureHeight = value;
      UpdateTexture();
    }
  }

  [Export]
  public float NoiseFrequency {
    get => _noiseFrequency;
    set {
      _noiseFrequency = value;
      UpdateTexture();
    }
  }

  [Export]
  public int NoiseOctaves {
    get => _noiseOctaves;
    set {
      _noiseOctaves = value;
      UpdateTexture();
    }
  }

  [Export]
  public float NoiseLacunarity {
    get => _noiseLacunarity;
    set {
      _noiseLacunarity = value;
      UpdateTexture();
    }
  }

  [Export]
  public float NoiseGain {
    get => _noiseGain;
    set {
      _noiseGain = value;
      UpdateTexture();
    }
  }

  [Export]
  public int NoiseSeed {
    get => _noiseSeed;
    set {
      _noiseSeed = value;
      UpdateTexture();
    }
  }

  [Export]
  public Vector2 TextureOffset {
    get => _textureOffset;
    set {
      _textureOffset = value;
      UpdateTexture();
    }
  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    UpdateTexture();
  }

  private void UpdateTexture() {
    GD.Print("Updating texture with size: " + _textureWidth + "x" + _textureHeight);
    if (_noise == null) return;

    // Update noise parameters
    _noise.Seed = _noiseSeed;
    _noise.Frequency = _noiseFrequency;
    _noise.FractalOctaves = _noiseOctaves;
    _noise.FractalLacunarity = _noiseLacunarity;
    _noise.FractalGain = _noiseGain;

    // Create a simple texture
    var image = Image.CreateEmpty(_textureWidth, _textureHeight, false, Image.Format.Rgba8);

    // Fill the image with noise pattern
    for (int y = 0; y < _textureHeight; y++) {
      for (int x = 0; x < _textureWidth; x++) {
        float rgb = (_noise.GetNoise2D(x + _textureOffset.X, y + _textureOffset.Y) + 1.0f) / 2.0f; // Normalize to [0, 1]
        var color = new Color(rgb, rgb, rgb, 1.0f);
        image.SetPixel(x, y, color);
      }
    }

    // Create texture from image
    var texture = ImageTexture.CreateFromImage(image);

    // Find or create the Sprite2D
    _sprite = GetNodeOrNull<Sprite2D>("TextureSprite");

    // Create new sprite if not found
    if (_sprite == null) {
      GD.Print("Creating new Sprite2D for texture display.");
      _sprite = new Sprite2D();
      _sprite.Name = "TextureSprite";
      AddChild(_sprite);
      if (Engine.IsEditorHint()) {
        _sprite.Owner = GetTree().EditedSceneRoot;
      }
    }
    _sprite.Texture = texture;
    _sprite.Position = new Vector2(
      _textureWidth / 2.0f,
      _textureHeight / 2.0f
    );
  }
}
