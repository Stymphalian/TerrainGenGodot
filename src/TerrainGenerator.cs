using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class TerrainGenerator : Node3D {
  [Export]
  public LevelOfDetailSetting[] detailLevels = [
    new LevelOfDetailSetting { lod = 0, distanceThreshold = 100 },
    new LevelOfDetailSetting { lod = 1, distanceThreshold = 250 },
    new LevelOfDetailSetting { lod = 4, distanceThreshold = 400 },
  ];
  [Export] public int ColliderLODMeshIndex = 1;
  [Export] public float playerMoveThresholdForChunkUpdate = 25.0f;

  [Export] public MeshSettings meshSettings;
  [Export] public HeightMapSettings heightMapSettings;
  [Export] public TextureData textureSettings;
  [Export] public Material terrainMaterial;

  private int chunksVisibleInViewDistance = 1;
  private Vector3 playerPosition;
  private Vector3 previousPlayerPosition;
  private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
  private HashSet<Vector2> visibleTerrainChunks = new HashSet<Vector2>();

  private int maxViewDist {
    get => detailLevels[^1].distanceThreshold;
  }
  private float meshChunkScale {
    get => meshSettings.MeshScale;
  }
  private float meshChunkSize {
    get => meshSettings.MeshWorldSize;
  }

  public override void _EnterTree() {
    base._EnterTree();
    meshSettings.Changed += () => {
      GD.Print("TerrainData changed, updating chunk scales");
      chunksVisibleInViewDistance = Mathf.CeilToInt((float)maxViewDist * meshChunkScale / meshChunkSize);
      terrainChunkDictionary.Values.ToList().ForEach(
        chunk => chunk.ResetTerrainChunk(meshChunkSize, meshChunkScale)
      );
    };
    heightMapSettings.Changed += () => {
      GD.Print("HeightMapSettings changed, resetting terrain chunks");
      terrainChunkDictionary.Values.ToList().ForEach(
        chunk => chunk.ResetTerrainChunk(meshChunkSize, meshChunkScale)
      );
    };
  }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    base._Ready();
    GD.Print("TerrainGenerator ready");
    textureSettings.ApplyToMaterial(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
    chunksVisibleInViewDistance = Mathf.CeilToInt((float)maxViewDist * meshChunkScale / meshChunkSize);
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
        chunk.UpdateCollisionMesh(playerPosition);
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
    var halfMeshChunkSize = meshChunkSize / 2;
    Vector2 playerChunkCoord = new Vector2(
      Mathf.FloorToInt((playerPosition.X - halfMeshChunkSize) / meshChunkSize) + 1,
      Mathf.FloorToInt((playerPosition.Z - halfMeshChunkSize) / meshChunkSize) + 1
    );
    // GD.Print($"Player chunk coord: {playerChunkCoord}");

    for(int index = visibleTerrainChunks.Count - 1; index >= 0; index--) {
      var coord = visibleTerrainChunks.ElementAt(index);
      terrainChunkDictionary[coord].UpdateTerrainChunk(playerPosition);
    }
    
    for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++) {
      for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++) {
        Vector2 viewedChunkCoord = new Vector2(
          playerChunkCoord.X + xOffset,
          playerChunkCoord.Y + yOffset
        );

        if (!terrainChunkDictionary.ContainsKey(viewedChunkCoord)) {
          TerrainChunk newChunk = new TerrainChunk(
            viewedChunkCoord,
            detailLevels,
            ColliderLODMeshIndex,
            meshSettings,
            heightMapSettings,
            terrainMaterial
          );
          terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
          newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
          newChunk.Load();
          GetTree().Root.AddChild(newChunk.Mesh);
          GetTree().Root.AddChild(newChunk.CollisionObject);
        }

        var chunk = terrainChunkDictionary[viewedChunkCoord];
        chunk.UpdateTerrainChunk(playerPosition);
      }
    }
  }

  public void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible) {
    if (isVisible) {
      visibleTerrainChunks.Add(chunk.ChunkCoords);
    } else {
      visibleTerrainChunks.Remove(chunk.ChunkCoords);
    }
  }
}
