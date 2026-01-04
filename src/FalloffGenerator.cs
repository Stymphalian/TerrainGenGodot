using System;
using Godot;

public partial class FalloffGenerator : Node {
    [Export] public Curve curve;

    public static float[,] GenerateFalloffMap(int size) {
        Curve curve = ResourceLoader.Load<Curve>("res://FalloffMapCurve.tres");

        float[,] falloffMap = new float[size, size];
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float nx = x / (float)size * 2 - 1;
                float ny = y / (float)size * 2 - 1;
                float value = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                // falloffMap[x, y] = value;
                falloffMap[x, y] = curve.Sample(value);
            }
        }
        return falloffMap;
    }
}
