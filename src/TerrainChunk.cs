using Godot;

public partial class TerrainChunk {
  public MeshInstance3D Mesh;
  public Aabb Bounds;
  public Vector2 Position;
  public int Size;
  private MapGenerator mapGeneratorRef;

  public TerrainChunk(MapGenerator mapGenRef, Vector2 position, int size) {
    mapGeneratorRef = mapGenRef;
    Mesh = new MeshInstance3D();
    Mesh.Name = "TerrainChunk at " + position;
    // Mesh.Mesh = new PlaneMesh();
    Mesh.Position = new Vector3(position.X, 0, position.Y);
    // Mesh.Scale = new Vector3(size, 1.0f, size);

    Bounds = new Aabb(
      new Vector3(position.X - size / 2.0f, 0, position.Y - size / 2.0f),
      new Vector3(size, 0, size)
    );
    Position = position;
    Size = size;

    mapGeneratorRef.RequestMapData(OnMapDataReceived);
  }

  private void OnMapDataReceived(MapData mapData) {
    GD.Print("Map data received in EndlessTerrain");
    // Request mesh data based on the map data
    mapGeneratorRef.RequestMeshData(mapData, OnMeshDataReceived);
  }

  private void OnMeshDataReceived(MeshData meshData) {
    GD.Print("Mesh data received in EndlessTerrain");
    Mesh.Mesh = meshData.CreateMesh();
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
