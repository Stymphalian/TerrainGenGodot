using Godot;

[GlobalClass]
public partial class TerrainType : Resource {
  [Export] public string Name;
  [Export] public Color Color;
  [Export] public float Height;
}