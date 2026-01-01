using Godot;
using System;

[GlobalClass]
public partial class MapDisplay : Node3D {

  [Export] public MeshInstance3D meshInstance {get; set;}

  public void DrawNoiseMap(float[,] noiseMap) {
    int width = noiseMap.GetLength(0);
    int height = noiseMap.GetLength(1);

    // Create a new texture and assign it to the MeshInstance which the child of this component
    var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);

    // Fill the image with noise pattern
    for (int y = 0; y < height; y++) {
      for (int x = 0; x < width; x++) {
        float rgb = noiseMap[x, y];
        var color = new Color(rgb, rgb, rgb, 1.0f);
        image.SetPixel(x, y, color);
      }
    }

    // Create texture from image
    var texture = ImageTexture.CreateFromImage(image);

    // Find the MeshInstance child
    if (meshInstance == null) {
      GD.PrintErr("MeshInstance3D node not found!");
      return;
    }
    
    meshInstance.SetSurfaceOverrideMaterial(0, new StandardMaterial3D {
      AlbedoTexture = texture
    });

    // Implementation to visualize the noise map
    GD.Print("Drawing noise map with dimensions: " + noiseMap.GetLength(0) + "x" + noiseMap.GetLength(1));
  }
}
