using Godot;

// [Tool]
public partial class MapGenerator : Node {
  private int mapWidth = 100;
  private int mapHeight = 100;
  private float noiseScale = 1.0f;

  [Export] public MapDisplay display { get; set; }

  [Export]
  public int MapWidth {
    get => mapWidth;
    set {
      mapWidth = value;
      GenerateMap();
    }
  }

  [Export]
  public int MapHeight {
    get => mapHeight;
    set {
      mapHeight = value;
      GenerateMap();
    }
  }

  [Export]
  public float NoiseScale {
    get => noiseScale;
    set {
      noiseScale = value;
      GenerateMap();
    }
  }

  public override void _Ready() {
    GenerateMap();
  }

  public void GenerateMap() {
    var noise = NoiseGenerator.Create();
    var noiseMap = NoiseGenerator.GenerateNoiseMap(noise, mapWidth, mapHeight, noiseScale);

    if (display == null) {
      GD.PrintErr("MapDisplay node not found!");
      return;
    }
    GD.PrintErr("MapDisplay node found!");
    display.DrawNoiseMap(noiseMap);
  }

}
