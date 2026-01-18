public partial class MapData
{
    public float MinHeight;
    public float MaxHeight;
    public float[,] HeightMap;

    public MapData(float minHeight, float maxHeight, float[,] heightMap)
    {
        HeightMap = heightMap;
        MinHeight = minHeight;
        MaxHeight = maxHeight;
    }
}