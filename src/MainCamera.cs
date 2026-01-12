using Godot;
using System;

public enum CameraMode
{
	FreeCamera,
	FollowPlayer
}

public partial class MainCamera : Camera3D
{
	[Export] public CameraMode Mode { get; set; } = CameraMode.FollowPlayer;
	[Export] public NodePath PlayerPath { get; set; }
	[Export] public Vector3 FollowOffset { get; set; } = new Vector3(0, 5, 10);
	[Export] public float FollowSmoothness { get; set; } = 5.0f;
	
	[Export] public float MoveSpeed { get; set; } = 10.0f;
	[Export] public float FastSpeedMultiplier { get; set; } = 2.0f;
	[Export] public float MouseSensitivity { get; set; } = 0.003f;

	private bool _isRotating = false;
	private Vector2 _lastMousePosition;
	private Node3D _player;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Mode == CameraMode.FollowPlayer && PlayerPath != null)
		{
			_player = GetNode<Node3D>(PlayerPath);
		}
	}

	public override void _Input(InputEvent @event)
	{
		// Only handle input if in FreeCamera mode
		if (Mode != CameraMode.FreeCamera)
			return;

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
		if (Mode == CameraMode.FreeCamera)
		{
			HandleMovement(delta);
		}
		else if (Mode == CameraMode.FollowPlayer && _player != null)
		{
			FollowPlayer(delta);
		}
	}

	private void FollowPlayer(double delta)
	{
		// Calculate target position behind the player
		var targetPosition = _player.GlobalPosition + _player.Transform.Basis * FollowOffset;
		
		// Smoothly interpolate to target position
		GlobalPosition = GlobalPosition.Lerp(targetPosition, FollowSmoothness * (float)delta);
		
		// Look at the player
		LookAt(_player.GlobalPosition, Vector3.Up);
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
