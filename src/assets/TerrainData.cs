using Godot;

[GlobalClass]
public partial class TerrainData : Resource {
  private float heightMultiplier = 10.0f;
  private Curve heightCurve;
  private bool useFalloffMap = false;
  private bool useFlatShading = false;
  private float terrainUniformScale = 2.5f;


  [Export]
  public float MeshHeightMultiplier {
    get => heightMultiplier;
    set {
      heightMultiplier = value;
      EmitChanged();
    }
  }

  [Export]
  public Curve HeightCurve {
    get => heightCurve;
    set {
      heightCurve = value;
      EmitChanged();
    }
  }

  [Export]
  public bool UseFalloffMap {
    get => useFalloffMap;
    set {
      useFalloffMap = value;
      EmitChanged();
    }
  }

  [Export]
  public bool UseFlatShading {
    get => useFlatShading;
    set {
      useFlatShading = value;
      EmitChanged();
    }
  }

  [Export]
  public float TerrainUniformScale {
    get => terrainUniformScale;
    set {
      terrainUniformScale = value;
      EmitChanged();  
    }
  }
}