using Godot;

[GlobalClass]
public partial class LevelOfDetailSetting : Resource {
  [Export] public int lod;
  [Export] public int distanceThreshold;
}