using Godot;

public partial class TerrainChunk {
  public MeshInstance3D Mesh;
  public Aabb Bounds;
  public Vector2 Position;
  public int Size;

  public TerrainChunk(Vector2 position, int size) {
    Mesh = new MeshInstance3D();
    Mesh.Name = "Plane";
    Mesh.Mesh = new PlaneMesh();
    Mesh.Position = new Vector3(position.X, 0, position.Y);
    Mesh.Scale = new Vector3(size, 1.0f, size);
    Bounds = new Aabb(
      new Vector3(position.X - size / 2.0f, 0, position.Y - size / 2.0f),
      new Vector3(size, 0, size)
    );
    Position = position;
    Size = size;
  }

  public void UpdateChunk(Vector3 playerPosition, float maxViewDist) {
    Vector3 center = Bounds.GetCenter();
    center.Y = 0.0f;
    Vector3 playerPos = playerPosition;
    playerPos.Y = 0.0f;

    float distanceToCenter = (center - playerPos).Length();
    bool visible = distanceToCenter <= maxViewDist;
    Mesh.Visible = visible;
  }

  public bool SetVisible(bool isVisible) {
    Mesh.Visible = isVisible;
    return isVisible;
  }

  public bool IsVisible() {
    return Mesh.Visible;
  }
}
