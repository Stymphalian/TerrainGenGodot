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
  
}