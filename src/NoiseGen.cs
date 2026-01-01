using System.Collections.Generic;
using Godot;

public static class NoiseGenerator {
  
  public static FastNoiseLite Create() {
    var noise = new FastNoiseLite();
    noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
    noise.Frequency = 0.05f;
    noise.FractalOctaves = 4;
    noise.FractalLacunarity = 2.0f;
    noise.FractalGain = 0.5f;
    return noise;
  }

  public static float[,] GenerateNoiseMap(FastNoiseLite noise, int width, int height, float scale  = 1.0f) {
    var points = new float[width, height];

    for (int x = 0; x < width; x++) {
      for (int z = 0; z < height; z++) {
        float sampleX = x * scale;
        float sampleZ = z * scale;
        float y = noise.GetNoise2D(sampleX, sampleZ) + 1.0f / 2.0f;
        points[x, z] = y;
      }
    }

    return points;
  }
  
}