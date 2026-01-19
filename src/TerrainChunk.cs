using Godot;


public partial class TerrainChunk {
  public MeshInstance3D Mesh;
  public StaticBody3D CollisionObject;
  public event System.Action<TerrainChunk, bool> OnVisibilityChanged;
  public static float colliderGenerationDistanceThreshold = 5.0f;
  public bool isDirty = true;

  private CollisionShape3D CollisionShape;
  private Rect2 Bounds;
  public Vector2 ChunkCoords;
  private Vector2 ChunkPosition;  
  private MapData mapData;
  private StandardMaterial3D material;
  private LevelOfDetailSetting[] LODSettings;
  private LODMesh[] lodInfos;
  private MeshSettings meshSettings;
  private HeightMapSettings heightMapSettings;
  private Material terrainMaterial;

  private int previousLODIndex = -1;
  private int colliderLODMeshIndex = 0;
  private bool hasReceivedMapData = false;
  private bool hasCollisionMesh = false;
  private bool visualizeLODWithMaterial = false;

  public TerrainChunk(
    Vector2 chunkCoords,
    LevelOfDetailSetting[] lodSettings,
    int colliderLODMeshIndex,
    MeshSettings meshSettings,
    HeightMapSettings heightMapSettings,
    Material terrainMaterial
    ) {
    this.meshSettings = meshSettings;
    this.heightMapSettings = heightMapSettings;
    this.terrainMaterial = terrainMaterial;

    LODSettings = lodSettings;
    ChunkCoords = chunkCoords;
    ChunkPosition = new Vector2(
      ChunkCoords.X * meshSettings.MeshWorldSize - meshSettings.MeshWorldSize / 2,
      ChunkCoords.Y * meshSettings.MeshWorldSize - meshSettings.MeshWorldSize / 2
    );
    this.colliderLODMeshIndex = colliderLODMeshIndex;
    Mesh = new MeshInstance3D();
    Mesh.Name = $"TerrainChunk at {ChunkCoords} {ChunkPosition}";
    Mesh.Mesh = new PlaneMesh();
    CollisionObject = new StaticBody3D();
    CollisionObject.Name = $"CollisionObject at {ChunkCoords} {ChunkPosition}";
    CollisionShape = new CollisionShape3D();
    CollisionObject.AddChild(CollisionShape);

    lodInfos = new LODMesh[lodSettings.Length];
    for (int i = 0; i < lodSettings.Length; i++) {
      lodInfos[i] = new LODMesh(lodSettings[i].lod);
      lodInfos[i].UpdateCallback += OnLODMeshReceived;
    }
  }

  public void Load() {
    ResetTerrainChunk(meshSettings.MeshWorldSize, meshSettings.MeshScale);
  }

  private void OnLODMeshReceived() {
    // GD.Print("LOD Mesh data received");
    isDirty = true;
  }

  private void OnMapDataReceived(MapData mapData) {
    // GD.Print("Map data received in EndlessTerrain");
    this.mapData = mapData;
    this.hasReceivedMapData = true;
    this.hasCollisionMesh = false;

    // Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.ColorMap);
    // Texture2D texture = TextureGenerator.TextureFromHeightMap(mapData.HeightMap);
    material = new StandardMaterial3D {
      // AlbedoTexture = texture,
      TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
      TextureRepeat = false,
      ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel, // Changed from PerVertex to PerPixel for proper lighting
      AlbedoColor = new Color(1, 0, 0), // Start with fully red (LOD 0)
    };

    if (visualizeLODWithMaterial) {
      Mesh.SetSurfaceOverrideMaterial(0, material);  
    } else {
      Mesh.SetSurfaceOverrideMaterial(0, terrainMaterial);
    }    
  }

  public int getLodIndex(float distanceToCenter) {
    int lodIndex = LODSettings.Length - 1;
    for (int i = 0; i < LODSettings.Length - 1; i++) {
      if (distanceToCenter <= LODSettings[i].distanceThreshold * meshSettings.MeshScale) {
        lodIndex = i;
        break;
      }
    }
    return lodIndex;
  }

  public void VisualizeLODWithMaterial(bool visualize) {
    if (visualize && material != null) {
      Mesh.SetSurfaceOverrideMaterial(0, material);
    } else {
      Mesh.SetSurfaceOverrideMaterial(0, terrainMaterial);
    }
  }

  public void ResetTerrainChunk(float meshWorldSize, float meshChunkScale) {
    GD.Print($"Resetting terrain chunk at {ChunkCoords}");
    hasCollisionMesh = false;
    hasReceivedMapData = false;
    previousLODIndex = -1;

    ChunkPosition = new Vector2(
      ChunkCoords.X * meshWorldSize - meshWorldSize / 2,
      ChunkCoords.Y * meshWorldSize - meshWorldSize / 2
    );
    Mesh.Position = new Vector3(ChunkPosition.X, 0, ChunkPosition.Y);
    CollisionShape.Position = new Vector3(ChunkPosition.X, 0, ChunkPosition.Y);
    Bounds = new Rect2(ChunkPosition, new Vector2(meshWorldSize, meshWorldSize));

    for (int i = 0; i < LODSettings.Length; i++) {
      lodInfos[i].HasReceivedMesh = false;
      lodInfos[i].HasRequestedMesh = false;
    }

    Vector2 chunkPositionNoScale = new Vector2(
      ChunkCoords.X * meshWorldSize / meshChunkScale,
      ChunkCoords.Y * meshWorldSize / meshChunkScale
    );
    ThreadedDataRequestor.RequestData(
      () => HeightMapGenerator.GenerateHeightMap(
        meshSettings.BorderedMeshSize, 
        meshSettings.BorderedMeshSize,
        heightMapSettings,
        chunkPositionNoScale
      ),
      (object o) => OnMapDataReceived(o as MapData)
    );
    
    // Mesh.Position = new Vector3(chunkPosition.X, 0, chunkPosition.Y) * terrainChunkScale;
    // Mesh.Scale = Vector3.One * terrainChunkScale;
    // CollisionShape.Position = new Vector3(chunkPosition.X, 0, chunkPosition.Y) * terrainChunkScale;
    // CollisionShape.Scale = Vector3.One * terrainChunkScale;
  }

  private float maxViewDist {
    get => LODSettings[LODSettings.Length - 1].distanceThreshold * meshSettings.MeshScale;
  }

  public void UpdateTerrainChunk(Vector3 playerPosition) {
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
    bool wasVisible = IsChunkVisible();
    bool visible = distanceToCenter <= maxViewDist;

    if (visible) {
      int lodIndex = getLodIndex(distanceToCenter);

      // Load the terrain mesh for this LOD if not already loaded
      if (previousLODIndex != lodIndex) {
        if (lodInfos[lodIndex].HasReceivedMesh) {
          GD.Print($"Updating chunk {ChunkCoords} from {previousLODIndex} LOD to {lodIndex}");
          previousLODIndex = lodIndex;
          Mesh.Mesh = lodInfos[lodIndex].Mesh;
          UpdateMaterialColor(lodIndex);
        } else if (!lodInfos[lodIndex].HasRequestedMesh) {
          lodInfos[lodIndex].RequestMesh(meshSettings, mapData);
        }
      }
    }

    if (wasVisible != visible) {
      SetChunkVisible(visible);
      OnVisibilityChanged?.Invoke(this, visible);
    }
  }

  private float collisionCheckDistance {
    get => colliderGenerationDistanceThreshold * meshSettings.MeshScale;
  }

  public void UpdateCollisionMesh(Vector3 playerPosition) {
    if (hasCollisionMesh) {
      return;
    }
    if (!hasReceivedMapData) {
      return;
    }

    Vector2 playerPos = new Vector2(playerPosition.X, playerPosition.Z);
    Vector2 closestPoint = new Vector2(
      Mathf.Clamp(playerPos.X, Bounds.Position.X, Bounds.Position.X + Bounds.Size.X),
      Mathf.Clamp(playerPos.Y, Bounds.Position.Y, Bounds.Position.Y + Bounds.Size.Y)
    );
    float distanceToBounds = (closestPoint - playerPos).Length();
    if (distanceToBounds > collisionCheckDistance) {
      return;
    }

    var collisionLODMesh = lodInfos[colliderLODMeshIndex];
    if (collisionLODMesh.HasReceivedMesh) {
      // GD.Print("Updating collision mesh ");
      CollisionShape.Shape = collisionLODMesh.Mesh.CreateTrimeshShape();
      hasCollisionMesh = true;
    } else if (collisionLODMesh.HasRequestedMesh == false) {
      // GD.Print("Requesting collision LOD mesh ");
      // CollisionShape.Shape = lodInfos[lodIndex].Mesh.CreateTrimeshShape();
      collisionLODMesh.RequestMesh(meshSettings, mapData);
    }
  }

  public bool SetChunkVisible(bool isVisible) {
    Mesh.Visible = isVisible;
    return isVisible;
  }

  public bool IsChunkVisible() {
    return Mesh.Visible;
  }

  private void UpdateMaterialColor(int lodIndex) {
    if (material == null) return;
    if (!visualizeLODWithMaterial) return;
    // LOD 0 = fully green (0, 1, 0)
    // As LOD increases, add lighter shades of red
    float increments = 1.0f / LODSettings.Length;
    float redComponent = increments + lodIndex * increments; // Increment red by 0.2 for each LOD level
    material.AlbedoColor = new Color(redComponent, 0, 0);
  }

  class LODMesh {
    public int LOD;
    public bool HasRequestedMesh = false;
    public bool HasReceivedMesh = false;
    // public MeshData meshData;
    public ArrayMesh Mesh;
    public event System.Action UpdateCallback;

    public LODMesh(int LOD) {
      this.LOD = LOD;
      // this.updateCallback = updateCallback;
    }

    public void RequestMesh(MeshSettings meshSettings, MapData mapData) {
      // GD.Print($"Requesting mesh for LOD {LOD}");
      // GD.Print($"Requesting mesh with {meshSettings} and mapData {mapData} for LOD {LOD}" );
      HasRequestedMesh = true;
      ThreadedDataRequestor.RequestData(
        () => MeshGenerator.GenerateTerrainMesh(
          mapData.HeightMap,
          meshSettings,
          LOD
        ),
        (object o) => OnMeshDataReceived(o as MeshData)
      );
      // mapGeneratorRef.RequestMeshData(mapData, LOD, OnMeshDataReceived);
    }

    public void OnMeshDataReceived(MeshData meshData) {
      // GD.Print($"Mesh data received for LOD {LOD}");
      HasReceivedMesh = true;
      Mesh = meshData.CreateMesh();
      UpdateCallback();
    }
  }
}
