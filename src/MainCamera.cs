using Godot;
using System;

public partial class MainCamera : Camera3D
{
	[Export] public float MoveSpeed { get; set; } = 10.0f;
	[Export] public float FastSpeedMultiplier { get; set; } = 2.0f;
	[Export] public float MouseSensitivity { get; set; } = 0.003f;

	private bool _isRotating = false;
	private Vector2 _lastMousePosition;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	public override void _Input(InputEvent @event)
	{
		// Handle mouse button press/release
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				_isRotating = mouseButton.Pressed;
				if (_isRotating)
				{
					_lastMousePosition = mouseButton.Position;
					Input.MouseMode = Input.MouseModeEnum.Captured;
				}
				else
				{
					Input.MouseMode = Input.MouseModeEnum.Visible;
				}
			}
		}

		// Handle mouse motion
		if (@event is InputEventMouseMotion mouseMotion && _isRotating)
		{
			var mouseDelta = mouseMotion.Relative;

			// Rotate around Y axis (yaw)
			RotateY(-mouseDelta.X * MouseSensitivity);

			// Rotate around X axis (pitch)
			RotateObjectLocal(Vector3.Right, -mouseDelta.Y * MouseSensitivity);

			// Clamp pitch to prevent flipping
			var rotation = RotationDegrees;
			rotation.X = Mathf.Clamp(rotation.X, -89, 89);
			RotationDegrees = rotation;
		}

    // Handle ESC to exit the program
    if (@event is InputEventKey keyEvent)
    {
        if (keyEvent.Keycode == Key.Escape && keyEvent.Pressed)
        {
            GetTree().Quit();
        }
    }
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		HandleMovement(delta);
	}

	private void HandleMovement(double delta)
	{
		var velocity = Vector3.Zero;
		var speed = MoveSpeed;

		// Get input direction
		if (Input.IsKeyPressed(Key.W))
		{
      // GD.Print("W pressed");
			velocity -= Transform.Basis.Z;
		}
		if (Input.IsKeyPressed(Key.S))
		{
      // GD.Print("S pressed");
			velocity += Transform.Basis.Z;
		}
		if (Input.IsKeyPressed(Key.A))
		{
      // GD.Print("A pressed");
			velocity -= Transform.Basis.X;
		}
		if (Input.IsKeyPressed(Key.D))
		{
      // GD.Print("D pressed");
			velocity += Transform.Basis.X;
		}
		if (Input.IsKeyPressed(Key.R))
		{
      // GD.Print("R pressed");
			velocity += Transform.Basis.Y;
		}
		if (Input.IsKeyPressed(Key.F))
		{
      // GD.Print("F pressed");
			velocity -= Transform.Basis.Y;
		}

		// Normalize and apply movement
		if (velocity.Length() > 0)
		{
			velocity = velocity.Normalized();
			Position += velocity * speed * (float)delta;
		}
	}
}
