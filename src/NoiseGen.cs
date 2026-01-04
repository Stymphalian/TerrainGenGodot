using System.Collections.Generic;
using Godot;

public static class NoiseGenerator {

  public static FastNoiseLite Create() {
    return CreateCustom(
      Seed: 0,
      Frequency: 0.05f,
      Octaves: 4,
      Lacunarity: 2.0f,
      Persistence: 0.5f
    );
  }

  public static FastNoiseLite CreateCustom(
    int Seed, float Frequency, int Octaves, float Lacunarity,
    float Persistence
  ) {
    var noise = new FastNoiseLite();
    noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex;
    noise.Seed = Seed;
    noise.Frequency = Frequency;
    noise.FractalOctaves = Octaves;
    noise.FractalLacunarity = Lacunarity;
    noise.FractalGain = Persistence;
    return noise;
  }

  public static float[,] GenerateNoiseMap(
    FastNoiseLite noise, int width, int height, float scale, Vector2 offset) {
    var points = new float[width, height];
    if (scale <= 0) {
      scale = 0.0001f;
    }

    float halfWidth = width / 2.0f;
    float halfHeight = height / 2.0f;

    for (int x = 0; x < width; x++) {
      for (int z = 0; z < height; z++) {
        float sampleX = (x - halfWidth + offset.X) / scale ;
        float sampleZ = (z - halfHeight + offset.Y) / scale ;
        float y = noise.GetNoise2D(sampleX, sampleZ);
        y = (y + 1.0f) / 2.0f;
        points[x, z] = y;
      }
    }

    return points;
  }

}