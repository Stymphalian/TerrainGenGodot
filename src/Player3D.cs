using Godot;
using System;

public partial class Player3D : CharacterBody3D
{
	[Export] public float MoveSpeed { get; set; } = 5.0f;
	[Export] public float JumpVelocity { get; set; } = 10.0f;
	[Export] public float MouseSensitivity { get; set; } = 0.003f;

	private float _gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
	private bool _isRotating = false;

	public override void _Input(InputEvent @event)
	{
		// Handle mouse button press/release for camera rotation
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				_isRotating = mouseButton.Pressed;
				if (_isRotating)
				{
					Input.MouseMode = Input.MouseModeEnum.Captured;
				}
				else
				{
					Input.MouseMode = Input.MouseModeEnum.Visible;
				}
			}
		}

		// Handle mouse motion for player rotation
		if (@event is InputEventMouseMotion mouseMotion && _isRotating)
		{
			var mouseDelta = mouseMotion.Relative;
			RotateY(-mouseDelta.X * MouseSensitivity);
		}

		// Handle ESC to exit the program
		if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.Keycode == Key.Escape && keyEvent.Pressed)
			{
				GetTree().Quit();
			} else if (keyEvent.Keycode == Key.Key0 && keyEvent.Pressed) {
        // Reset global positin to origin at 300 height
        GlobalPosition = new Vector3(0, 300, 0);
        GD.Print("Player position: " + GlobalPosition);
      }
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		var velocity = Velocity;

		// Apply gravity
		if (!IsOnFloor())
		{
			velocity.Y -= _gravity * (float)delta;
		}

		// Handle jump
		if (Input.IsKeyPressed(Key.Space) && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get input direction
		var inputDir = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W))
			inputDir.Y -= 1;
		if (Input.IsKeyPressed(Key.S))
			inputDir.Y += 1;
		if (Input.IsKeyPressed(Key.A))
			inputDir.X -= 1;
		if (Input.IsKeyPressed(Key.D))
			inputDir.X += 1;

		// Calculate movement direction relative to player rotation
		var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();

		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * MoveSpeed;
			velocity.Z = direction.Z * MoveSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, MoveSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, MoveSpeed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
