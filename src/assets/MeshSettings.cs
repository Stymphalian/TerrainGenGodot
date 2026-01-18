using System;
using Godot;

[GlobalClass]
public partial class MeshSettings : Resource {

  public const int NumSupportedLODs = 5;
  public const int NumSupportedChunkSizes = 9;
  public static readonly int[] SupportedChunkSizes = {47, 71, 95, 119, 143, 167, 191, 215, 239};

  private bool useFlatShading = false;
  private float meshScale = 2.5f;

  [Export]
  public bool UseFlatShading {
    get => useFlatShading;
    set {
      useFlatShading = value;
      EmitChanged();
    }
  }

  [Export]
  public float MeshScale {
    get => meshScale;
    set {
      meshScale = value;
      EmitChanged();  
    }
  }

  public int chunkSizeIndex;
  [Export] int ChunkSizeIndex {
    get => chunkSizeIndex;
    set {
      chunkSizeIndex = Math.Clamp(value, 0, SupportedChunkSizes.Length - 1);
      EmitChanged();
    }
  }

  // This size of the chunk not including the border vertices
  public int MeshSize {
    get => SupportedChunkSizes[chunkSizeIndex];
  }

  public int BorderedMeshSize {
    get => MeshSize + 2;
  }

  public float MeshWorldSize {
    // Minus-1 because chunksSize is odd and we want equal halfChunkSizes to be equal so we can center the chunks
    get => (MeshSize - 1) * meshScale;
  }
}