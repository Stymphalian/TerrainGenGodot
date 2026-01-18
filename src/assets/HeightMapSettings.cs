using Godot;

[GlobalClass]
public partial class HeightMapSettings : Resource {
  private NoiseSettings noiseSettings;
  private float heightMultiplier = 1.0f;
  private Curve heightCurve;
  private bool useFalloffMap = false;

  public float MinHeight {
    get {
      return heightMultiplier * heightCurve.GetMinValue();
    }
  }

  public float MaxHeight {
    get {
      return heightMultiplier * heightCurve.GetMaxValue();
    }
  }

  [Export]
  public NoiseSettings NoiseSettings {
    get => noiseSettings;
    set {
      noiseSettings = value;
      noiseSettings.Changed += EmitChanged;
    }
  }

  [Export(PropertyHint.Range, "0.0,1000.0")]
  public float MeshHeightMultiplier
  {
    get => heightMultiplier;
    set
    {
      heightMultiplier = value;
      EmitChanged();
    }
  }

  [Export]
  public Curve HeightCurve
  {
    get => heightCurve;
    set
    {
      heightCurve = value;
      heightCurve.Changed += EmitChanged;
      EmitChanged();
    }
  }

  [Export]
  public bool UseFalloffMap
  {
    get => useFalloffMap;
    set
    {
      useFalloffMap = value;
      EmitChanged();
    }
  }
}