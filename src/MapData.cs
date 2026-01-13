using Godot;

public struct MapData {
  public readonly float[,] HeightMap;
  public readonly Color[,] ColorMap;

  // public MapData(float[,] heightMap, Color[,] colorMap) {
  public MapData(float[,] heightMap) {
    HeightMap = heightMap;
    // ColorMap = colorMap;
  }
}