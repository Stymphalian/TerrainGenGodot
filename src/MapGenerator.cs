using Godot;
using Godot.Collections;

// [Tool]
public partial class MapGenerator : Node {
  public enum DRAW_MODE {
    NOISE_MAP,
    COLOR_MAP,
  };


  private int mapWidth = 100;
  private int mapHeight = 100;
  private Vector2 noiseOffset = new Vector2(0, 0);
  private int noiseSeed = 0;
  private float noiseScale = 1.0f;
  private int noiseOctaves = 4;
  private float noiseFrequency = 0.05f;
  private float noiseLacunarity = 2.0f;
  private float noisePersistence = 0.5f;
  private DRAW_MODE drawMode = DRAW_MODE.NOISE_MAP;
  private TerrainType[] regions = [
    new TerrainType { Name = "Water Deep", Color = new Color(0, 0, 0.75f), Height = 0.3f },
    new TerrainType { Name = "Water Shallow", Color = new Color(0, 0, 0.8f), Height = 0.4f },
    new TerrainType { Name = "Sand", Color = new Color(0.76f, 0.7f, 0.5f), Height = 0.45f },
    new TerrainType { Name = "Grass", Color = new Color(0.1f, 0.6f, 0.1f), Height = 0.55f },
    new TerrainType { Name = "Grass 2", Color = new Color(0.0f, 0.5f, 0.0f), Height = 0.6f },
    new TerrainType { Name = "Rock", Color = new Color(0.55f, 0.4f, 0.25f), Height = 0.7f },
    new TerrainType { Name = "Rock 2", Color = new Color(0.45f, 0.3f, 0.2f), Height = 0.8f },
    new TerrainType { Name = "Snow", Color = new Color(1.0f, 1.0f, 1.0f), Height = 1.0f } 
  ];


  [Export] public MapDisplay display { get; set; }

  [Export] public DRAW_MODE DrawMode {
    get => drawMode;
    set {
      drawMode = value;
      GenerateMap();
    }
  }
  
  [Export(PropertyHint.Range, "1,1024")]
  public int MapWidth {
    get => mapWidth;
    set {
      mapWidth = value;
      GenerateMap();
    }
  }

  [Export(PropertyHint.Range, "1,1024")]
  public int MapHeight {
    get => mapHeight;
    set {
      mapHeight = value;
      GenerateMap();
    }
  }

  [Export(PropertyHint.Range, "0.0001,100.0,0.0001")]
  public float NoiseScale {
    get => noiseScale;
    set {
      noiseScale = value;
      GenerateMap();
    }
  }

  [Export(PropertyHint.Range, "0.0001,100.0,0.0001")]
  public float NoiseFrequency {
    get => noiseFrequency;
    set {
      noiseFrequency = value;
      GenerateMap();
    }
  }

  [Export(PropertyHint.Range, "1,16")]
  public int NoiseOctaves {
    get => noiseOctaves;
    set {
      noiseOctaves = value;
      GenerateMap();
    }
  }

  [Export(PropertyHint.Range, "0.0001,100.0,0.0001")]
  // Lacunarity: frequency multiplier between octaves
  public float NoiseLacunarity {
    get => noiseLacunarity;
    set {
      noiseLacunarity = value;
      GenerateMap();
    }
  }

  [Export(PropertyHint.Range, "0.0,1.0,0.001")]
  public float NoisePersistence {
    get => noisePersistence;
    set {
      noisePersistence = value;
      GenerateMap();
    }
  }

  [Export]
  public Vector2 NoiseOffset {
    get => noiseOffset;
    set {
      noiseOffset = value;
      GenerateMap();
    }
  }

  [Export]
  public int NoiseSeed {
    get => noiseSeed;
    set {
      noiseSeed = value;
      GenerateMap();
    }
  }

  [Export] public TerrainType[] Regions {
    get => regions;
    set {
      regions = value;
      GenerateMap();
    }
  }

  public override void _Ready() {
    GenerateMap();
  }

  public Color[,] GetColorMapFromHeighMap(float[,] heightMap) {
    Color[,] colorMap = new Color[mapWidth, mapHeight];
    for (int y = 0; y < mapHeight; y++) {
      for (int x = 0; x < mapWidth; x++) {
        float currentHeight = heightMap[x, y];

        foreach (var region in Regions) {
          if (currentHeight <= region.Height) {
            colorMap[x, y] = region.Color;
            break;
          }
        }

      }
    }
    return colorMap;
  }

  public void GenerateMap() {
    FastNoiseLite noise = NoiseGenerator.CreateCustom(
      noiseSeed, noiseFrequency, noiseOctaves,
      noiseLacunarity, noisePersistence
    );
    float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(
      noise, mapWidth, mapHeight, noiseScale, noiseOffset);
    Color[,] colorMap = GetColorMapFromHeighMap(noiseMap);

    if (display == null) {
      GD.PrintErr("MapDisplay node not found!");
      return;
    }

    Texture2D texture;
    if (drawMode == DRAW_MODE.COLOR_MAP) {
      texture = TextureGenerator.TextureFromColorMap(colorMap);
    } else {
      texture = TextureGenerator.TextureFromHeightMap(noiseMap);
    }
    display.DrawTexture(texture);
  }

}
