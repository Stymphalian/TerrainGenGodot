using System;
using System.Globalization;
using System.Threading;
using Godot;


public partial class TerrainChunk {
  public MeshInstance3D Mesh;
  public Rect2 Bounds;
  public Vector2 Position;
  public int ChunkSize;
  private MapData mapData;
  private StandardMaterial3D material;
  private bool hasReceivedMapData = false;
  private MapGenerator mapGeneratorRef;
  private LevelOfDetailSetting[] lodSettings;
  private LODMesh[] lodInfos;
  private int previousLODIndex = -1;
  public bool isDirty = true;

  public TerrainChunk(
    MapGenerator mapGenRef,
    Vector2 chunkCoords,
    Vector2 chunkPosition,
    int chunkSize,
    LevelOfDetailSetting[] lodSettings,
    float terrainChunkScale
    ) {
    mapGeneratorRef = mapGenRef;
    this.lodSettings = lodSettings;
    Mesh = new MeshInstance3D();
    Mesh.Name = $"TerrainChunk at {chunkCoords} {chunkPosition}";
    Mesh.Mesh = new PlaneMesh();
    // Mesh.Position = new Vector3(chunkPosition.X + chunkSize/2, 0, chunkPosition.Y + chunkSize/2);
    Mesh.Position = new Vector3(chunkPosition.X, 0, chunkPosition.Y) * terrainChunkScale;
    Mesh.Scale = Vector3.One * terrainChunkScale;

    Bounds = new Rect2(chunkPosition, new Vector2(chunkSize, chunkSize));
    Position = chunkPosition;
    ChunkSize = chunkSize;

    lodInfos = new LODMesh[lodSettings.Length];
    for (int i = 0; i < lodSettings.Length; i++) {
      lodInfos[i] = new LODMesh(lodSettings[i].lod, OnLODMeshReceived);
    }

    mapGeneratorRef.RequestMapData(chunkPosition, OnMapDataReceived);
  }

  private void OnLODMeshReceived() {
    GD.Print("LOD Mesh data received");
    isDirty = true;
  }

  private void OnMapDataReceived(MapData mapData) {
    GD.Print("Map data received in EndlessTerrain");
    this.mapData = mapData;
    this.hasReceivedMapData = true;

    Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.ColorMap);
    // Texture2D texture = TextureGenerator.TextureFromHeightMap(mapData.HeightMap);
    material = new StandardMaterial3D {
      AlbedoTexture = texture,
      TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
      TextureRepeat = false,
      ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel, // Changed from PerVertex to PerPixel for proper lighting
      // AlbedoColor = new Color(1, 0, 0), // Start with fully red (LOD 0)
    };
    Mesh.SetSurfaceOverrideMaterial(0, material);
  }

  public int getLodIndex(float distanceToCenter) {
    int lodIndex = lodSettings.Length - 1;
    for (int i = 0; i < lodSettings.Length - 1; i++) {
      if (distanceToCenter <= lodSettings[i].distanceThreshold) {
        lodIndex = i;
        break;
      }
    }
    return lodIndex;
  }

  public void UpdateChunk(Vector3 playerPosition, float maxViewDist) {
    if (!hasReceivedMapData) {
      return;
    }
    Vector2 playerPos = new Vector2(playerPosition.X, playerPosition.Z);

    // Calculate distance to closest point on bounds
    Vector2 closestPoint = new Vector2(
      Mathf.Clamp(playerPos.X, Bounds.Position.X, Bounds.Position.X + Bounds.Size.X),
      Mathf.Clamp(playerPos.Y, Bounds.Position.Y, Bounds.Position.Y + Bounds.Size.Y)
    );
    float distanceToCenter = (closestPoint - playerPos).Length();
    bool visible = distanceToCenter <= maxViewDist;
    Mesh.Visible = visible;

    if (visible) {
      int lodIndex = getLodIndex(distanceToCenter);
      if (!lodInfos[lodIndex].HasRequestedMesh) {
        lodInfos[lodIndex].RequestMesh(mapGeneratorRef, mapData);
        return;
      }
      if (previousLODIndex == lodIndex) {
        return;
      }
      if (lodInfos[lodIndex].HasReceivedMesh) {
        // GD.Print($"Updating LOD to {lodIndex}");
        previousLODIndex = lodIndex;
        Mesh.Mesh = lodInfos[lodIndex].Mesh;
        // UpdateMaterialColor(lodIndex);
      }
    }
  }

  public bool SetVisible(bool isVisible) {
    Mesh.Visible = isVisible;
    return isVisible;
  }

  public bool IsVisible() {
    return Mesh.Visible;
  }

  private void UpdateMaterialColor(int lodIndex) {
    if (material == null) return;
    // LOD 0 = fully green (0, 1, 0)
    // As LOD increases, add lighter shades of red
    float increments = 1.0f / lodSettings.Length;
    float redComponent = increments + lodIndex * increments; // Increment red by 0.2 for each LOD level
    material.AlbedoColor = new Color(redComponent, 0, 0);
  }

  class LODMesh {
    public int LOD;
    public bool HasRequestedMesh = false;
    public bool HasReceivedMesh = false;
    // public MeshData meshData;
    public ArrayMesh Mesh;
    private System.Action updateCallback;

    public LODMesh(int LOD, System.Action updateCallback) {
      this.LOD = LOD;
      this.updateCallback = updateCallback;
    }

    public void RequestMesh(MapGenerator mapGeneratorRef, MapData mapData) {
      // GD.Print($"Requesting mesh for LOD {LOD}");
      HasRequestedMesh = true;
      mapGeneratorRef.RequestMeshData(mapData, LOD, OnMeshDataReceived);
    }

    public void OnMeshDataReceived(MeshData meshData) {
      // GD.Print($"Mesh data received for LOD {LOD}");
      HasReceivedMesh = true;
      Mesh = meshData.CreateMesh();
      updateCallback();
    }
  }
}
