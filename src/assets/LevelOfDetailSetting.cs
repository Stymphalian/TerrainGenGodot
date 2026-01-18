using Godot;

[GlobalClass]
public partial class LevelOfDetailSetting : Resource {
  [Export(PropertyHint.Range, $"0,4")] public int lod;
  [Export] public int distanceThreshold;
  public bool useForCollision = false;
}