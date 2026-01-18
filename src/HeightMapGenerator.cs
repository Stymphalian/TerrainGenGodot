using Godot;
using System.Diagnostics;

public partial class HeightMapGenerator
{
  public static MapData GenerateHeightMap(
    int width,
    int height,
    HeightMapSettings settings,
    Vector2 topLeft)
  {
    NoiseSettings noiseSettings = settings.NoiseSettings;
    FastNoiseLite noise = NoiseGenerator.CreateCustom(
      noiseSettings.noiseSeed,
      noiseSettings.noiseFrequency,
      noiseSettings.noiseOctaves,
      noiseSettings.noiseLacunarity,
      noiseSettings.noisePersistence
    );

    float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(
      noise,
      width,
      height,
      noiseSettings.noiseScale,
      topLeft + noiseSettings.noiseOffset);

    float minHeight = float.MaxValue;
    float maxHeight = float.MinValue;
    for (int y = 0; y < noiseMap.GetLength(1); y++) {
      for (int x = 0; x < noiseMap.GetLength(0); x++) {
        noiseMap[x, y] = settings.MeshHeightMultiplier * settings.HeightCurve.Sample(noiseMap[x, y]);
        minHeight = Mathf.Min(minHeight, noiseMap[x, y]);
        maxHeight = Mathf.Max(maxHeight, noiseMap[x, y]);
      }
    }

    // TODO: Apply falloff map

    return new MapData(minHeight, maxHeight, noiseMap);
  }
}
