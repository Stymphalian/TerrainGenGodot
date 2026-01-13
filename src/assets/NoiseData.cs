using Godot;

[GlobalClass]
public partial class NoiseData : Resource {
  [Export] public Vector2 noiseOffset = new Vector2(0, 0);
  [Export] public int noiseSeed = 0;
  public float noiseScale = 1.0f;
  public int noiseOctaves = 4;
  public float noiseFrequency = 0.0075f;
  public float noiseLacunarity = 2.0f;
  public float noisePersistence = 0.5f;

  [Export(PropertyHint.Range, "0.0001,100.0,0.0001")]
  public float NoiseScale {
    get => noiseScale;
    set {
      noiseScale = value;
      EmitChanged();
    }
  }

  [Export(PropertyHint.Range, "0.0001,100.0,0.0001")]
  public float NoiseFrequency {
    get => noiseFrequency;
    set {
      noiseFrequency = value;
      EmitChanged();
    }
  }

  [Export(PropertyHint.Range, "1,16")]
  public int NoiseOctaves {
    get => noiseOctaves;
    set {
      noiseOctaves = value;
      EmitChanged();
    }
  }

  [Export(PropertyHint.Range, "0.0001,100.0,0.0001")]
  // Lacunarity: frequency multiplier between octaves
  public float NoiseLacunarity {
    get => noiseLacunarity;
    set {
      noiseLacunarity = value;
      EmitChanged();
    }
  }

  [Export(PropertyHint.Range, "0.0,1.0,0.001")]
  public float NoisePersistence {
    get => noisePersistence;
    set {
      noisePersistence = value;
      EmitChanged();
    }
  }

  [Export]
  public Vector2 NoiseOffset {
    get => noiseOffset;
    set {
      noiseOffset = value;
      EmitChanged();
    }
  }

  [Export]
  public int NoiseSeed {
    get => noiseSeed;
    set {
      noiseSeed = value;
      EmitChanged();
    }
  }
}