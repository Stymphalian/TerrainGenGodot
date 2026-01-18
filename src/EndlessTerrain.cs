using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class EndlessTerrain : Node3D {
  [Export]
  public LevelOfDetailSetting[] detailLevels = [
    new LevelOfDetailSetting { lod = 0, distanceThreshold = 100 },
    new LevelOfDetailSetting { lod = 1, distanceThreshold = 250 },
    new LevelOfDetailSetting { lod = 4, distanceThreshold = 400 },
  ];
  [Export] public int ColliderLODMeshIndex = 1;
  [Export] public float playerMoveThresholdForChunkUpdate = 25.0f;
  [Export] public float colliderGenerationDistanceThreshold = 5.0f;

  private float chunkWorldSize = 0;
  private float halfChunkWorldSize = 0;
  private int chunksVisibleInViewDistance = 1;
  private Vector3 playerPosition;
  private Vector3 previousPlayerPosition;
  private MapGenerator mapGeneratorRef;

  private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
  private HashSet<Vector2> visibleTerrainChunks = new HashSet<Vector2>();


  private int maxViewDist {
    get => detailLevels[^1].distanceThreshold;
  }

  private float meshChunkScale {
    get {
      return mapGeneratorRef.MeshSettings.MeshScale;
    }
  }

  public override void _EnterTree() {
    base._EnterTree();
    mapGeneratorRef = GetNode<MapGenerator>("/root/Root/MapGenerator");
    mapGeneratorRef.MeshSettings.Changed += () => {
      GD.Print("TerrainData changed, updating chunk scales");
      chunkWorldSize = mapGeneratorRef.MeshSettings.MeshWorldSize;
      halfChunkWorldSize = mapGeneratorRef.MeshSettings.MeshWorldSize / 2;
      chunksVisibleInViewDistance = Mathf.CeilToInt((float)maxViewDist * meshChunkScale / chunkWorldSize);
      terrainChunkDictionary.Values.ToList().ForEach(
        chunk => chunk.ResetTerrainChunk(
          mapGeneratorRef.MeshSettings.MeshWorldSize,
          mapGeneratorRef.MeshSettings.MeshScale
        )
      );
    };
    mapGeneratorRef.HeightMapSettings.Changed += () => {
      GD.Print("HeightMapSettings changed, resetting terrain chunks");
      terrainChunkDictionary.Values.ToList().ForEach(
        chunk => chunk.ResetTerrainChunk(
          mapGeneratorRef.MeshSettings.MeshWorldSize,
          mapGeneratorRef.MeshSettings.MeshScale
        )
      );
    };
    chunkWorldSize = mapGeneratorRef.MeshSettings.MeshWorldSize;
    halfChunkWorldSize = mapGeneratorRef.MeshSettings.MeshWorldSize / 2;
  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    base._Ready();
    GD.Print("EndlessTerrain ready");
    chunksVisibleInViewDistance = Mathf.CeilToInt((float)maxViewDist * meshChunkScale / chunkWorldSize);
    playerPosition = GetViewport().GetCamera3D().Position;
    previousPlayerPosition = playerPosition;
  }

  bool firstProcess = true;
  public override void _Process(double delta) {
    base._Process(delta);
    playerPosition = GetViewport().GetCamera3D().Position;

    if (playerPosition != previousPlayerPosition) {
      foreach (var coord in visibleTerrainChunks) {
        TerrainChunk chunk = terrainChunkDictionary[coord];
        chunk.UpdateCollisionMesh(playerPosition, colliderGenerationDistanceThreshold * meshChunkScale);
      }
    }

    bool terrainChunksDirty = terrainChunkDictionary.Values.Any(chunk => chunk.isDirty);
    if (firstProcess || terrainChunksDirty || (playerPosition - previousPlayerPosition).Length() > playerMoveThresholdForChunkUpdate) {
      previousPlayerPosition = playerPosition;
      UpdateVisibleChunks();
      firstProcess = false;
    }
  }

  public void UpdateVisibleChunks() {
    Vector2 playerChunkCoord = new Vector2(
      Mathf.FloorToInt((playerPosition.X - halfChunkWorldSize) / chunkWorldSize) + 1,
      Mathf.FloorToInt((playerPosition.Z - halfChunkWorldSize) / chunkWorldSize) + 1
    );
    // GD.Print($"Player chunk coord: {playerChunkCoord}");

    // Hide previously visible chunks
    foreach (var coord in visibleTerrainChunks) {
      terrainChunkDictionary[coord].SetChunkVisible(false);
    }
    visibleTerrainChunks.Clear();

    for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
      for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
        Vector2 viewedChunkCoord = new Vector2(
          playerChunkCoord.X + xOffset,
          playerChunkCoord.Y + yOffset
        );

        if (!terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
          TerrainChunk newChunk = new TerrainChunk(
            mapGeneratorRef,
            viewedChunkCoord,
            chunkWorldSize,
            meshChunkScale,
            detailLevels,
            ColliderLODMeshIndex
          );
          terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
          GetTree().Root.AddChild(newChunk.Mesh);
          GetTree().Root.AddChild(newChunk.CollisionObject);
        }

        var chunk = terrainChunkDictionary[viewedChunkCoord];
        chunk.UpdateTerrainChunk(playerPosition, maxViewDist * meshChunkScale);
        if (chunk.IsChunkVisible()) {
          visibleTerrainChunks.Add(viewedChunkCoord);
        }
      }
    }
  }
}
