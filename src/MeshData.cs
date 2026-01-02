using Godot;
using Godot.Collections;


public class MeshData
{
  public Vector3[] Vertices;
  public Vector2[] UVs;
  public int[] Triangles;
  private int triangleIndex = 0;

  public MeshData(int meshWidth, int meshHeight)
  {
    Vertices = new Vector3[meshWidth * meshHeight];
    UVs = new Vector2[meshWidth * meshHeight];
    Triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
  }

  public void AddCWTriangle(int a, int b, int c)
  {
    Triangles[triangleIndex] = a;
    Triangles[triangleIndex + 1] = b;
    Triangles[triangleIndex + 2] = c;
    triangleIndex += 3;
  }

  public ArrayMesh CreateMesh()
  {
    ArrayMesh mesh = new ArrayMesh();
    Array arrays = new Array();
    arrays.Resize((int)Mesh.ArrayType.Max);

    arrays[(int)Mesh.ArrayType.Vertex] = Vertices;
    arrays[(int)Mesh.ArrayType.TexUV] = UVs;
    arrays[(int)Mesh.ArrayType.Index] = Triangles;

    mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
    return mesh;
  }
}