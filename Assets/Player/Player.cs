using Godot;
using System;
using System.Threading.Tasks;

public partial class Player : CharacterBody3D
{
	[Export] public float Speed = 5.0f;
	[Export] public float JumpVelocity = 4.5f;
	[Export] public float LookSensitivity = 5.0f;
	
	[Export] public Node3D Head { get; set; }
	[Export] public Camera3D Camera { get; set; }

	[Export] public float InteractRange = 2.0f;

	[Export] public Node3D MoebiusShaderQuad;
	
	public static Player PlayerInstance { get; set; }

	public bool bDisablePlayerInput = false;

	[Signal]
	public delegate void JumpedEventHandler();

	public override void _Ready()
	{
		base._Ready();

		PlayerInstance = this;

		PrintAfterJump();
		
		Input.SetMouseMode(Input.MouseModeEnum.Captured);
	}
	
	private async void PrintAfterJump()
	{
		while (true)
		{
			await ToSignal(this, SignalName.Jumped);
			GD.Print("Jumped!");
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed("interact"))
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

			if (@event.IsActionPressed("interact"))
			{
				// Get the center of the viewport screen
				Vector2 screenSize = GetViewport().GetVisibleRect().Size;
				Vector2 centerScreen = screenSize / 2f;

				// Calculate origin and direction from the camera
				Vector3 from = Camera.ProjectRayOrigin(centerScreen);
				Vector3 to = from + Camera.ProjectRayNormal(centerScreen) * InteractRange;

				// Query the physics space
				var spaceState = GetWorld3D().DirectSpaceState;
				var query = PhysicsRayQueryParameters3D.Create(from, to);
				query.CollideWithAreas = true;
				var result = spaceState.IntersectRay(query);

				if (result.Count > 0)
				{
					// Interact if we hit an Interactable object
					var hitCollider = (Node3D)result["collider"];
					if (hitCollider is Interactable interactableObject)
					{
						GetViewport().SetInputAsHandled();
						interactableObject.Interact();
					}
				}
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
		if (IsOnFloor() && Input.IsActionJustPressed("jump") && !bDisablePlayerInput)
		{
			newVelocity.Y = JumpVelocity;
			EmitSignal(SignalName.Jumped);
		}

		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = bDisablePlayerInput ? Vector2.Zero : Input.GetVector("move_left", "move_right", "move_forward", "move_back");
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
