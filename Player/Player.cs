using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float LookSensitivity = 5.0f;
	
	[Export] public Camera3D Camera { get; set; }
	
	public override void _Ready()
	{
		base._Ready();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton)
		{
			Input.SetMouseMode(Input.MouseModeEnum.Captured); // capture mouse when clicking on window
		}
		else if (@event.IsActionPressed("ui_cancel"))
		{
			Input.SetMouseMode(Input.MouseModeEnum.Visible); // release mouse capture
		}

		if (Input.GetMouseMode() == Input.MouseModeEnum.Captured)
		{
			if(@event is InputEventMouseMotion mouseEvent)
			{
				var scaledLookSensitivity = LookSensitivity * 0.001f; 
				RotateY(-mouseEvent.Relative.X * scaledLookSensitivity);
				Camera.RotateX(-mouseEvent.Relative.Y * scaledLookSensitivity);
				Camera.SetRotation(new Vector3(Math.Clamp(Camera.Rotation.X, Mathf.DegToRad(-90), Mathf.DegToRad(90)),
					Camera.Rotation.Y, Camera.Rotation.Z));
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 newVelocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			newVelocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("jump"))
		{
			newVelocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			newVelocity.X = direction.X * Speed;
			newVelocity.Z = direction.Z * Speed;
		}
		else
		{
			newVelocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			newVelocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = newVelocity;
		MoveAndSlide();
	}
}
