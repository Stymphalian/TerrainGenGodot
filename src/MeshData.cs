using Godot;
using Godot.Collections;


public class MeshData {
  public Vector3[] Vertices;
  public Vector3[] Normals;
  public Vector2[] UVs;
  public int[] Triangles;
  public Vector3[] BorderVertices;
  public int[] BorderTriangles;
  public int meshLength;

  private int triangleIndex = 0;
  private int borderTriangleIndex = 0;
  private bool useFlatShading = false;

  public MeshData(int meshLength, bool useFlatShading) {
    this.useFlatShading = useFlatShading;
    this.meshLength = meshLength;

    Vertices = new Vector3[meshLength * meshLength];
    Normals = new Vector3[Vertices.Length];
    UVs = new Vector2[meshLength * meshLength];
    Triangles = new int[(meshLength - 1) * (meshLength - 1) * 6];

    BorderVertices = new Vector3[(meshLength + 2)*4 - 4];
    BorderTriangles = new int[6*4*meshLength];
  }

  public void AddVertex(int index, Vector3 vertex, Vector2 uv) {
    if (index < 0) {
      BorderVertices[-index - 1] = vertex;
    } else {
      Vertices[index] = vertex;
      UVs[index] = uv;
    }
  }

  public void AddTriangle(int a, int b, int c) {
    bool containsBorder = a < 0 || b < 0 || c < 0;
    if (containsBorder) {
      BorderTriangles[borderTriangleIndex] = a;
      BorderTriangles[borderTriangleIndex + 1] = b;
      BorderTriangles[borderTriangleIndex + 2] = c;
      borderTriangleIndex += 3;
    } else {
      Triangles[triangleIndex] = a;
      Triangles[triangleIndex + 1] = b;
      Triangles[triangleIndex + 2] = c;
      triangleIndex += 3;
    }
  }

  public void FinishMesh() {
    if (useFlatShading) {
      ConvertToFlatShading();
    }
    CalculateNormals();  
  }

  void ConvertToFlatShading() {
    Vector3[] flatShadedVertices = new Vector3[Triangles.Length];
    Vector2[] flatShadedUVs = new Vector2[Triangles.Length];

    for(int index = 0; index < Triangles.Length; index++) {
      flatShadedVertices[index] = Vertices[Triangles[index]];
      flatShadedUVs[index] = UVs[Triangles[index]];
      Triangles[index] = index;
    }

    Vertices = flatShadedVertices;
    UVs = flatShadedUVs;
    Normals = new Vector3[Vertices.Length];
    
  }

  void CalculateNormals() {
    for (int i = 0; i < Triangles.Length; i += 3) {
      int vertexIndexA = Triangles[i];
      int vertexIndexB = Triangles[i + 1];
      int vertexIndexC = Triangles[i + 2];

      Vector3 pointA = Vertices[vertexIndexA];
      Vector3 pointB = Vertices[vertexIndexB];
      Vector3 pointC = Vertices[vertexIndexC];

      Vector3 sideAB = pointB - pointA;
      Vector3 sideAC = pointC - pointA;
      Vector3 triangleNormal = sideAC.Cross(sideAB).Normalized();

      Normals[vertexIndexA] += triangleNormal;
      Normals[vertexIndexB] += triangleNormal;
      Normals[vertexIndexC] += triangleNormal;
    }

    for (int i = 0; i < BorderTriangles.Length; i += 3) {
      int vertexIndexA = BorderTriangles[i];
      int vertexIndexB = BorderTriangles[i + 1];
      int vertexIndexC = BorderTriangles[i + 2];

      Vector3 pointA = (vertexIndexA < 0) ? BorderVertices[-vertexIndexA - 1] : Vertices[vertexIndexA];
      Vector3 pointB = (vertexIndexB < 0) ? BorderVertices[-vertexIndexB - 1] : Vertices[vertexIndexB];
      Vector3 pointC = (vertexIndexC < 0) ? BorderVertices[-vertexIndexC - 1] : Vertices[vertexIndexC];

      Vector3 sideAB = pointB - pointA;
      Vector3 sideAC = pointC - pointA;
      Vector3 triangleNormal = sideAC.Cross(sideAB).Normalized();

      if (vertexIndexA >= 0) {
        Normals[vertexIndexA] += triangleNormal;
      }
      if (vertexIndexB >= 0) {
        Normals[vertexIndexB] += triangleNormal;
      }
      if (vertexIndexC >= 0) {
        Normals[vertexIndexC] += triangleNormal;
      }
    }

    for (int i = 0; i < Normals.Length; i++) {
      Normals[i] = Normals[i].Normalized();
    }
  }

  public ArrayMesh CreateMesh() {
    ArrayMesh mesh = new ArrayMesh();
    Array arrays = new Array();
    arrays.Resize((int)Mesh.ArrayType.Max);

    // CalculateNormals();
    arrays[(int)Mesh.ArrayType.Vertex] = Vertices;
    arrays[(int)Mesh.ArrayType.TexUV] = UVs;
    arrays[(int)Mesh.ArrayType.Index] = Triangles;
    arrays[(int)Mesh.ArrayType.Normal] = Normals;  

    mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
    return mesh;
  }

  public ArrayMesh CreateNormalVisualizationMesh(float normalLength = 1.0f) {
    // Create a mesh with lines showing the normals
    Vector3[] lineVertices = new Vector3[Vertices.Length * 2];
    int[] lineIndices = new int[Vertices.Length * 2];

    for (int i = 0; i < Vertices.Length; i++) {
      // Start point of the line (vertex position)
      lineVertices[i * 2] = Vertices[i];
      // End point of the line (vertex position + normal * magnitude)
      lineVertices[i * 2 + 1] = Vertices[i] + Normals[i] * normalLength;

      // Line indices
      lineIndices[i * 2] = i * 2;
      lineIndices[i * 2 + 1] = i * 2 + 1;
    }

    ArrayMesh mesh = new ArrayMesh();
    Array arrays = new Array();
    arrays.Resize((int)Mesh.ArrayType.Max);
    arrays[(int)Mesh.ArrayType.Vertex] = lineVertices;
    arrays[(int)Mesh.ArrayType.Index] = lineIndices;

    mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
    return mesh;
  }
}
