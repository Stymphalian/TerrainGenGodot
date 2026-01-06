using Godot;
using System;

public partial class DirectionLight : DirectionalLight3D
{
  [Export] float distance = 200.0f;
  [Export] float degreesPerSecond = 10.0f;
  [Export] bool rotateAroundX = true;
  private float timeElapsed = 0.0f;

  public override void _Ready()
  {
  }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
    // timeElapsed += (float)delta;
    // float angle = Mathf.DegToRad(timeElapsed * degreesPerSecond);
    // float xPos = Mathf.Cos(angle) * distance;
    // float zPos = Mathf.Sin(angle) * distance;
    // Position = new Vector3(xPos, Position.Y, zPos);

    if (rotateAroundX) {
      RotateX(Mathf.DegToRad((float)delta * degreesPerSecond));
    }

  }
}
