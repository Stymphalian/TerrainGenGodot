using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class EndlessTerrain : Node3D
{
  // [Export] public int maxViewDist = 500;
  [Export] public LevelOfDetailSetting[] detailLevels = new LevelOfDetailSetting[] {
    new LevelOfDetailSetting { lod = 0, distanceThreshold = 100 },
    new LevelOfDetailSetting { lod = 2, distanceThreshold = 250 },
    new LevelOfDetailSetting { lod = 4, distanceThreshold = 500 },
    new LevelOfDetailSetting { lod = 8, distanceThreshold = 800 },
  };
  [Export] public float playerMoveThresholdForChunkUpdate = 25.0f;
  private int chunkSize = MapGenerator.mapChunkSize - 1;
  private int chunksVisibleInViewDistance = 1;
  private Vector3 playerPosition;
  private Vector3 previousPlayerPosition;
  private MapGenerator mapGeneratorRef;
  
  private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
  private HashSet<Vector2> visibleTerrainChunks = new HashSet<Vector2>();

  public int maxViewDist {
    get =>  detailLevels[^1].distanceThreshold;
  }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
    GD.Print("EndlessTerrain ready");
    chunkSize = MapGenerator.mapChunkSize - 1;
    chunksVisibleInViewDistance = Mathf.CeilToInt((float)maxViewDist / chunkSize);
    playerPosition = GetViewport().GetCamera3D().Position;
    previousPlayerPosition = playerPosition;
    Position = GetViewport().GetCamera3D().Position;

    mapGeneratorRef = GetNode<MapGenerator>("/root/Root/MapGenerator");
  }

  bool firstProcess = true;
  public override void _Process(double delta) {
    base._Process(delta);
    playerPosition = GetViewport().GetCamera3D().Position;
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
      Mathf.FloorToInt(playerPosition.X / chunkSize),
      Mathf.FloorToInt(playerPosition.Z / chunkSize)
    );

    // Hide previously visible chunks
    foreach(var coord in visibleTerrainChunks) {
      terrainChunkDictionary[coord].SetVisible(false);
    }
    visibleTerrainChunks.Clear();

    for(int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
      for(int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
        Vector2 viewedChunkCoord = new Vector2(
          playerChunkCoord.X + xOffset,
          playerChunkCoord.Y + yOffset
        );

        if (!terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
          TerrainChunk newChunk = new TerrainChunk(
            mapGeneratorRef,
            new Vector2(
              viewedChunkCoord.X * chunkSize,
              viewedChunkCoord.Y * chunkSize
            ),
            chunkSize,
            detailLevels
          );
          terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
          GetTree().Root.AddChild(newChunk.Mesh);
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