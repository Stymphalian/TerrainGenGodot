using Godot;

public static class MeshGenerator {

  public const int numSupportedLODs = 5;
  public const int numSupportedChunkSizes = 9;
  public static readonly int[] supportedChunkSizes = {47, 71, 95, 119, 143, 167, 191, 215, 239};

  // Generate the terrain mesh from the height map. Starting from 0,0 as the top-left corner
  // and extending to width,height as the bottom-right corner.
  // This is to match the TerrainChunk generation.
  public static MeshData GenerateTerrainMesh(
    float[,] heightMap,
    float heightMultiplier,
    Curve heightCurve,
    int levelOfDetail,
    bool useFlatShading) {

    int meshLODIncr = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
    int borderLength = heightMap.GetLength(0);
    int meshLength = borderLength - 2 * meshLODIncr;
    int meshLengthOriginal = borderLength - 2;
    int verticesPerLine = (meshLength - 1) / meshLODIncr + 1;

    // Create a map of the vertex and border vertex indices
    int[,] vertexIndices = new int[borderLength, borderLength];
    int meshVertexIndex = 0;
    int borderVertexIndex = -1;
    for (int z = 0; z < borderLength; z += meshLODIncr) {
      for (int x = 0; x < borderLength; x += meshLODIncr) {
        bool isBorderVertex = z == 0 || z == borderLength - 1 || x == 0 || x == borderLength - 1;
        if (isBorderVertex) {
          vertexIndices[x, z] = borderVertexIndex;
          borderVertexIndex--;
        } else {
          vertexIndices[x, z] = meshVertexIndex;
          meshVertexIndex++;
        }
      }
    }

    MeshData meshData = new MeshData(verticesPerLine, useFlatShading);
    for (int z = 0; z < borderLength; z += meshLODIncr) {
      for (int x = 0; x < borderLength; x += meshLODIncr) {
        float heightMult = heightCurve.Sample(heightMap[x, z]) * heightMultiplier;
        float heightValue = heightMap[x, z] * heightMult;
        Vector2 uv = new Vector2(
          (x - meshLODIncr) / (float)meshLength, (z - meshLODIncr) / (float)meshLength
        );
        Vector3 vec = new Vector3(
          uv.X * meshLengthOriginal, heightValue, uv.Y * meshLengthOriginal);

        int vertexIndex = vertexIndices[x, z];
        meshData.AddVertex(vertexIndex, vec, uv);
        if (x < borderLength - 1 && z < borderLength - 1) {
          int tl = vertexIndices[x, z];
          int tr = vertexIndices[x + meshLODIncr, z];
          int bl = vertexIndices[x, z + meshLODIncr];
          int br = vertexIndices[x + meshLODIncr, z + meshLODIncr];
          meshData.AddTriangle(tl, tr, bl);
          meshData.AddTriangle(tr, br, bl);
        }
      }
    }
    meshData.FinishMesh();

    // Return or use the generated mesh as needed
    return meshData;
  }
}
