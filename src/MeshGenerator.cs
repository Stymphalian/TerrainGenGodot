using System;
using Godot;

public static class MeshGenerator
{
  public static MeshData GenerateTerrainMesh(float[,] heightMap)
  {
    int width = heightMap.GetLength(0);
    int height = heightMap.GetLength(1);
    float halfWidth = width / 2.0f;
    float halfHeight = height / 2.0f;

    MeshData meshData = new MeshData(width, height);
    int vertexIndex = 0;
    for (int z = 0; z < height; z++)
    {
      for (int x = 0; x < width; x++)
      {
        float heightValue = heightMap[x, z];
        meshData.Vertices[vertexIndex] = new Vector3(x - halfWidth, heightValue, z - halfHeight);
        meshData.UVs[vertexIndex] = new Vector2(x / (float)width, z / (float)height);

        if (x < width - 1 && z < height - 1)
        {
          meshData.AddCWTriangle(
            vertexIndex, // top-left
            vertexIndex + 1,
            vertexIndex + width
          );
          meshData.AddCWTriangle(
            vertexIndex + 1, // top-right
            vertexIndex + width + 1,
            vertexIndex + width
          );
        }

        vertexIndex++;
      }
    }

    // Return or use the generated mesh as needed 
    return meshData;
  }
}