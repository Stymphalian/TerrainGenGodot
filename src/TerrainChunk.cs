using System;
using System.Globalization;
using System.Threading;
using Godot;


public partial class TerrainChunk {
  public MeshInstance3D Mesh;
  public Rect2 Bounds;
  public Vector2 Position;
  public int Size;
  private MapData mapData;
  private StandardMaterial3D material;
  private bool hasReceivedMapData = false;
  private MapGenerator mapGeneratorRef;
  private LevelOfDetailSetting[] lodSettings;
  private LODMesh[] lodInfos;
  private int previousLODIndex = -1;
  public bool isDirty = true;

  public TerrainChunk(
    MapGenerator mapGenRef, Vector2 chunkPosition, int size,
    LevelOfDetailSetting[] lodSettings
    ) {
    mapGeneratorRef = mapGenRef;
    this.lodSettings = lodSettings;
    Mesh = new MeshInstance3D();
    Mesh.Name = "TerrainChunk at " + chunkPosition;
    Mesh.Mesh = new PlaneMesh();
    Mesh.Position = new Vector3(chunkPosition.X, 0, chunkPosition.Y);
    // Mesh.Scale = new Vector3(size, 1.0f, size);

    Bounds = new Rect2(
      chunkPosition,
      new Vector2(size, size)
    );
    Position = chunkPosition;
    Size = size;

    lodInfos = new LODMesh[lodSettings.Length];
    for(int i = 0; i < lodSettings.Length; i++) {
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
    material = new StandardMaterial3D {
      AlbedoTexture = texture,
      TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
      TextureRepeat = false,
    };
    Mesh.SetSurfaceOverrideMaterial(0, material);
  }

  public int getLodIndex(float distanceToCenter) {
    int lodIndex = lodSettings.Length -1;
    for(int i = 0; i < lodSettings.Length-1; i++) {
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
    Vector2 center = Bounds.GetCenter();
    Vector2 playerPos = new Vector2(playerPosition.X, playerPosition.Z);

    float distanceToCenter = (center - playerPos).Length();
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
        GD.Print($"Updating LOD to {lodIndex}");
        previousLODIndex = lodIndex;
        Mesh.Mesh = lodInfos[lodIndex].Mesh;
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

  class LODMesh {
    public int LOD;
    public bool HasRequestedMesh = false;
    public bool HasReceivedMesh = false;
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
