using Godot;

[Tool]
public partial class PerlinMesh : MeshInstance3D
{
  private int _width = 100;
  private int _height = 100;
  private float _heightMultiplier = 10.0f;

  private int _noiseSeed = 0;
  private float _noiseFrequency = 0.01f;
  private int _noiseOctaves = 4;
  private float _noiseLacunarity = 2.0f;
  private float _noiseGain = 0.5f;
  private Vector2 _textureOffset = new Vector2(0, 0);

  [Export] 
  public int Width { 
    get => _width;
    set {
      _width = value;
      GenerateMesh();
    }
  }
  
  [Export] 
  public int Height { 
    get => _height;
    set {
      _height = value;
      GenerateMesh();
    }
  }

  [Export]
  public float HeightMultiplier {
    get => _heightMultiplier;
    set {
      _heightMultiplier = value;
      GenerateMesh();
    }
  }


  [Export]
  public float NoiseFrequency {
    get => _noiseFrequency;
    set {
      _noiseFrequency = value;
      GenerateMesh();
    }
  }

  [Export]
  public int NoiseOctaves {
    get => _noiseOctaves;
    set {
      _noiseOctaves = value;
      GenerateMesh();
    }
  }

  [Export]
  public float NoiseLacunarity {
    get => _noiseLacunarity;
    set {
      _noiseLacunarity = value;
      GenerateMesh();
    }
  }

  [Export]
  public float NoiseGain {
    get => _noiseGain;
    set {
      _noiseGain = value;
      GenerateMesh();
    }
  }

  [Export]
  public int NoiseSeed {
    get => _noiseSeed;
    set {
      _noiseSeed = value;
      GenerateMesh();
    }
  }

  [Export]
  public Vector2 TextureOffset {
    get => _textureOffset;
    set {
      _textureOffset = value;
      GenerateMesh();
    }
  }


	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
    GenerateMesh();
  }

  public Color ColorFromHeight(float height) {
    switch (height) {
      case < 0.3f:
        return Colors.Blue;
      case < 0.35f:
        return Colors.Yellow;
      case < 0.5f:
        return Colors.Green;
      case < 0.7f:
        return Colors.Brown;
      case < 0.9f:
        return Colors.Gray;
      default:
        return Colors.White;
    }
  }

  public void GenerateMesh() {
    return;

    // var noise = NoiseGenerator.Create();
    // noise.Seed = _noiseSeed;
    // noise.Frequency = _noiseFrequency;
    // noise.FractalOctaves = _noiseOctaves;
    // noise.FractalLacunarity = _noiseLacunarity;
    // noise.FractalGain = _noiseGain;

    // var heightMap = NoiseGenerator.GenerateNoiseMap(noise, _width, _height);
    // var st = new SurfaceTool();
    // st.Begin(Mesh.PrimitiveType.Triangles);

    // for (int x = 0; x < _width - 1; x++)
    // {
    //   for (int z = 0; z < _height - 1; z++)
    //   {
    //     // First triangle
    //     st.SetNormal(Vector3.Up);
    //     st.SetColor(ColorFromHeight(heightMap[x, z]));
    //     st.AddVertex(new Vector3(x, heightMap[x, z] * _heightMultiplier, z));
    //     st.SetNormal(Vector3.Up);
    //     st.SetColor(ColorFromHeight(heightMap[x + 1, z]));
    //     st.AddVertex(new Vector3(x + 1, heightMap[x + 1, z] * _heightMultiplier, z));
    //     st.SetNormal(Vector3.Up);
    //     st.SetColor(ColorFromHeight(heightMap[x, z + 1]));
    //     st.AddVertex(new Vector3(x, heightMap[x, z + 1] * _heightMultiplier, z + 1));
    //     // Second triangle
    //     st.SetNormal(Vector3.Up);
    //     st.SetColor(ColorFromHeight(heightMap[x + 1, z]));
    //     st.AddVertex(new Vector3(x + 1, heightMap[x + 1, z] * _heightMultiplier, z));
    //     st.SetNormal(Vector3.Up);
    //     st.SetColor(ColorFromHeight(heightMap[x + 1, z + 1]));
    //     st.AddVertex(new Vector3(x + 1, heightMap[x + 1, z + 1] * _heightMultiplier, z + 1));
    //     st.SetNormal(Vector3.Up);
    //     st.SetColor(ColorFromHeight(heightMap[x, z + 1]));
    //     st.AddVertex(new Vector3(x, heightMap[x, z + 1] * _heightMultiplier, z + 1));
    //   }
    // }

    // var mesh = st.Commit();
    // this.Mesh = mesh;
  }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
