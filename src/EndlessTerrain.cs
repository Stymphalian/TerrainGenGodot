using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class EndlessTerrain : Node3D {
  // [Export] public int maxViewDist = 500;
  // [Export] public float TerrainChunkScale = 1.0f;
  [Export]
  public LevelOfDetailSetting[] detailLevels = [
    new LevelOfDetailSetting { lod = 0, distanceThreshold = 100 },
    new LevelOfDetailSetting { lod = 1, distanceThreshold = 250 , useForCollision = true},
    new LevelOfDetailSetting { lod = 4, distanceThreshold = 400 },
    // new LevelOfDetailSetting { lod = 4, distanceThreshold = 1600 },
  ];
  [Export] public float playerMoveThresholdForChunkUpdate = 25.0f;
  private int chunkSize = MapGenerator.mapChunkSize - 1;
  private int halfChunkSize = (MapGenerator.mapChunkSize - 1) / 2;
  private int chunksVisibleInViewDistance = 1;
  private Vector3 playerPosition;
  private Vector3 previousPlayerPosition;
  private MapGenerator mapGeneratorRef;

  private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
  private HashSet<Vector2> visibleTerrainChunks = new HashSet<Vector2>();

  public int maxViewDist {
    get => detailLevels[^1].distanceThreshold;
  }

  public float TerrainChunkScale {
    get {
      return mapGeneratorRef.TerrainData.TerrainUniformScale;
    }
  }

  public override void _EnterTree() {
    base._EnterTree();
    mapGeneratorRef = GetNode<MapGenerator>("/root/Root/MapGenerator");
    mapGeneratorRef.TerrainData.Changed += () => {
      GD.Print("TerrainData changed, updating chunk scales");
      terrainChunkDictionary.Values.ToList().ForEach(
        chunk => chunk.UpdateTerrainChunkScale(TerrainChunkScale)
      );
    };
  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    base._Ready();
    GD.Print("EndlessTerrain ready");
    chunkSize = MapGenerator.mapChunkSize - 1;
    chunksVisibleInViewDistance = Mathf.CeilToInt((float)maxViewDist / chunkSize);
    playerPosition = GetViewport().GetCamera3D().Position / TerrainChunkScale;
    previousPlayerPosition = playerPosition;
    Position = GetViewport().GetCamera3D().Position;
  }

  bool firstProcess = true;
  public override void _Process(double delta) {
    base._Process(delta);
    playerPosition = GetViewport().GetCamera3D().Position / TerrainChunkScale;
    Position = GetViewport().GetCamera3D().Position;

    bool terrainChunksDirty = terrainChunkDictionary.Values.Any(chunk => chunk.isDirty);
    if (firstProcess || terrainChunksDirty || (playerPosition - previousPlayerPosition).Length() > playerMoveThresholdForChunkUpdate) {
      previousPlayerPosition = playerPosition;
      UpdateVisibleChunks();
      firstProcess = false;
    }
  }

  public void UpdateVisibleChunks() {
    Vector2 playerChunkCoord = new Vector2(
      Mathf.FloorToInt((playerPosition.X - halfChunkSize) / chunkSize) + 1,
      Mathf.FloorToInt((playerPosition.Z - halfChunkSize) / chunkSize) + 1
    );
    // GD.Print($"Player chunk coord: {playerChunkCoord}");

    // Hide previously visible chunks
    foreach (var coord in visibleTerrainChunks) {
      terrainChunkDictionary[coord].SetVisible(false);
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
            new Vector2(
              viewedChunkCoord.X * chunkSize - halfChunkSize,
              viewedChunkCoord.Y * chunkSize - halfChunkSize
            ),
            chunkSize,
            detailLevels,
            TerrainChunkScale
          );
          terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
          GetTree().Root.AddChild(newChunk.Mesh);
          GetTree().Root.AddChild(newChunk.CollisionObject);
          // CollisionObject is already a child of Mesh, don't add it separately
          // GD.Print($"Chunk at {viewedChunkCoord} position {newChunk.Position}");
        }

        var chunk = terrainChunkDictionary[viewedChunkCoord];
        chunk.UpdateChunk(playerPosition, maxViewDist);
        if (chunk.IsVisible()) {
          visibleTerrainChunks.Add(viewedChunkCoord);
        }
      }
    }
  }
}
