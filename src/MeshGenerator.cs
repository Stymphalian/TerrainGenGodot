using System;
using Godot;

public static class MeshGenerator
{
  // Generate the terrain mesh from the height map. Starting from 0,0 as the top-left corner
  // and extending to width,height as the bottom-right corner.
  // This is to match the TerrainChunk generation.
  public static MeshData GenerateTerrainMesh(
    float[,] heightMap, float heightMultiplier, Curve heightCurve, int levelOfDetail)
  {
    int width = heightMap.GetLength(0);
    int height = heightMap.GetLength(1);
    // float halfWidth =  (width - 1) / 2.0f;
    // float halfHeight = (height - 1) / 2.0f;
    int simplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
    int verticesPerLine = (width - 1) / simplificationIncrement + 1;

    MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
    int vertexIndex = 0;
    for (int z = 0; z < height; z += simplificationIncrement)
    {
      for (int x = 0; x < width; x += simplificationIncrement)
      {
        float heightMult = heightCurve.Sample(heightMap[x, z]) * heightMultiplier;
        float heightValue = heightMap[x, z] * heightMult;
        // meshData.Vertices[vertexIndex] = new Vector3(x + halfWidth, heightValue, z + halfHeight);
        meshData.Vertices[vertexIndex] = new Vector3(x, heightValue, z);
        meshData.UVs[vertexIndex] = new Vector2(x / (float)width, z / (float)height);

        if (x < width - 1 && z < height - 1)
        {
          meshData.AddCWTriangle(
            vertexIndex, // top-left
            vertexIndex + 1,
            vertexIndex + verticesPerLine
          );
          meshData.AddCWTriangle(
            vertexIndex + 1, // top-right
            vertexIndex + verticesPerLine + 1,
            vertexIndex + verticesPerLine
          );
        }

        vertexIndex++;
      }
    }

    // Return or use the generated mesh as needed 
    return meshData;
  }
}