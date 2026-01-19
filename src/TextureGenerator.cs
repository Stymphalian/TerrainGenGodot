using Godot;

public static class TextureGenerator {
  
  public static Texture2D TextureFromColorMap(Color[,] colorMap) {
      int width = colorMap.GetLength(0);
      int height = colorMap.GetLength(1);

      // Create a new texture and assign it to the MeshInstance which the child of this component
      var image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);

      // Fill the image with noise pattern
      for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
          image.SetPixel(x, y, colorMap[x, y]);
        }
      }

      // Create texture from image
      ImageTexture texture = ImageTexture.CreateFromImage(image);
      return texture;
  }

  public static Texture2D TextureFromHeightMap(MapData mapData) {
      int width = mapData.HeightMap.GetLength(0);
      int height = mapData.HeightMap.GetLength(1);

      // Create a new texture and assign it to the MeshInstance which the child of this component
      Color[,] colorMap = new Color[width, height];

      // Fill the image with noise pattern
      for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
          float rgb = mapData.HeightMap[x, y] / (mapData.MaxHeight - mapData.MinHeight);
          colorMap[x, y] = new Color(rgb, rgb, rgb, 1.0f);;
        }
      }

      return TextureFromColorMap(colorMap);
  }
}