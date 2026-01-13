using Godot;

public static class MeshGenerator {
  // Generate the terrain mesh from the height map. Starting from 0,0 as the top-left corner
  // and extending to width,height as the bottom-right corner.
  // This is to match the TerrainChunk generation.
  public static MeshData GenerateTerrainMesh(
    float[,] heightMap, float heightMultiplier, Curve heightCurve, int levelOfDetail, bool useFlatShading) {
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

  // Generate a UV sphere mesh with specified radius and resolution, optionally using a heightMap for displacement
  public static MeshData GenerateSphereMesh(float[,] heightMap = null, float baseRadius = 1.0f, float heightMultiplier = 1.0f, int latitudeSegments = 32, int longitudeSegments = 32) {
    int vertexCount = (latitudeSegments + 1) * (longitudeSegments + 1);
    MeshData meshData = new MeshData(longitudeSegments + 1, false);

    int vertexIndex = 0;

    int heightMapWidth = heightMap?.GetLength(0) ?? 1;
    int heightMapHeight = heightMap?.GetLength(1) ?? 1;

    // Generate vertices
    for (int lat = 0; lat <= latitudeSegments; lat++) {
      float theta = lat * Mathf.Pi / latitudeSegments; // 0 to PI
      float sinTheta = Mathf.Sin(theta);
      float cosTheta = Mathf.Cos(theta);

      for (int lon = 0; lon <= longitudeSegments; lon++) {
        float phi = lon * 2.0f * Mathf.Pi / longitudeSegments; // 0 to 2PI
        float sinPhi = Mathf.Sin(phi);
        float cosPhi = Mathf.Cos(phi);

        // Spherical to Cartesian coordinates
        float x = cosPhi * sinTheta;
        float y = cosTheta;
        float z = sinPhi * sinTheta;

        Vector3 direction = new Vector3(x, y, z); // Direction from center

        // Sample heightMap if provided
        float radiusAtPoint = baseRadius;
        if (heightMap != null) {
          float u = (float)lon / longitudeSegments;
          float v = (float)lat / latitudeSegments;

          int heightX = Mathf.Clamp((int)(u * (heightMapWidth - 1)), 0, heightMapWidth - 1);
          int heightY = Mathf.Clamp((int)(v * (heightMapHeight - 1)), 0, heightMapHeight - 1);

          float heightValue = heightMap[heightX, heightY];
          radiusAtPoint = baseRadius + (heightValue * heightMultiplier);
        }

        meshData.Vertices[vertexIndex] = direction * radiusAtPoint;
        meshData.Normals[vertexIndex] = direction; // Will be recalculated properly later
        meshData.UVs[vertexIndex] = new Vector2((float)lon / longitudeSegments, (float)lat / latitudeSegments);

        // Generate triangles (skip last row and column for triangle generation)
        if (lat < latitudeSegments && lon < longitudeSegments) {
          int current = lat * (longitudeSegments + 1) + lon;
          int next = current + longitudeSegments + 1;

          // First triangle (clockwise winding)
          meshData.AddTriangle(current, next, current + 1);
          // Second triangle (clockwise winding)
          meshData.AddTriangle(current + 1, next, next + 1);
        }

        vertexIndex++;
      }
    }

    meshData.FinishMesh();
    return meshData;
  }
}
