using Godot;
using System;
using System.Collections;
using System.Numerics;

namespace Godot.Collections;

[GlobalClass]
public partial class Keypad : Interactable
{
	[Export] public Camera3D Camera;
	[Export] public Node3D KeypadMeshes;
	[Export] public PackedScene KeypadUi;
	[Export] public Curve ButtonPressCurve;
	[Export] public float CameraBlendDuration = 0.5f;

	private Array<Node> buttonMeshes = [];
	private IEnumerator pressButtonTask;
	private bool bUsingKeypad = false;
	private Control keypadUiInstance;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach(var mesh in KeypadMeshes.GetChildren())
		{
			if (buttonMeshes.Count < 12)
			{
				buttonMeshes.Add(mesh);
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (pressButtonTask != null)
		{
			bool keepGoing = pressButtonTask.MoveNext();
			if (!keepGoing)
			{
				pressButtonTask = null;
			}
		}
	}
	
	// Called when any input event occurs (UI, mouse, keyboard, etc.)
	public override void _Input(InputEvent @event)
	{
		if (bUsingKeypad)
		{
			if (@event.IsActionPressed("interact"))
			{
				GetViewport().SetInputAsHandled();
				
				// Raycast from mouse position
				Vector2 mousePosition = GetViewport().GetMousePosition();
				Camera3D camera = GetViewport().GetCamera3D();

				Vector3 from = camera.ProjectRayOrigin(mousePosition);
				Vector3 to = from + (camera.ProjectRayNormal(mousePosition) * 500.0f);

				var spaceState = GetWorld3D().DirectSpaceState;
				var query = PhysicsRayQueryParameters3D.Create(from, to);
				query.CollideWithAreas = false;
				var result = spaceState.IntersectRay(query);

				if (result.Count > 0)
				{
					// Interact with buttons
					var hitCollider = (Node3D)result["collider"];
					if (buttonMeshes.Contains(hitCollider.GetParent()))
					{
						if (pressButtonTask == null)
						{
							pressButtonTask = PressButton(buttonMeshes.IndexOf(hitCollider.GetParent()));
						}
					}
				}
				
			}
			if (@event.IsActionPressed("ui_cancel"))
			{
				ExitKeypad();
			}
		}
	}

	public override void Interact()
	{
		EnterKeypad();
	}
	
	private IEnumerator PressButton(int buttonIndex)
	{
		float dt = (float)GetProcessDeltaTime();
		float moveTime = 0.2f;
		float moveDistance = 0.01f;
		Node3D buttonMesh = (Node3D)buttonMeshes[buttonIndex];
		Vector3 start = buttonMesh.GetPosition();

		for (float i = 0.0f; i < 1.0f; i+=dt/moveTime)
		{
			Vector3 targetPosition = new Vector3(start.X, start.Y, start.Z - ButtonPressCurve.Sample(i)*moveDistance);
			buttonMesh.SetPosition(targetPosition);
			yield return null;
		}
	}

	private void EnterKeypad()
	{
		// Ensure the active camera matches the start position explicitly if needed
		Tween tween = CreateTween().SetParallel(true).SetTrans(Tween.TransitionType.Cubic);

		// Interpolate main player camera to target view
		tween.TweenProperty(Player.PlayerInstance.Camera, "global_position", Camera.GlobalPosition, CameraBlendDuration);
		tween.TweenProperty(Player.PlayerInstance.Camera, "global_rotation", Camera.GlobalRotation, CameraBlendDuration);

		Player.PlayerInstance.bDisablePlayerInput = true;
		keypadUiInstance = KeypadUi.Instantiate<Control>();
		AddChild(keypadUiInstance);
		Button exitButton = (Button)keypadUiInstance.FindChild("ExitButton");
		GD.Print(exitButton.Name);
		exitButton.Pressed += ExitKeypad;
		bUsingKeypad = true;
		Input.SetMouseMode(Input.MouseModeEnum.Visible);
	}

	private void ExitKeypad()
	{
		// Ensure the active camera matches the start position explicitly if needed
		Tween tween = CreateTween().SetParallel(true).SetTrans(Tween.TransitionType.Cubic);

		// Interpolate main player camera to target view
		tween.TweenProperty(Player.PlayerInstance.Camera, "position", Vector3.Zero, CameraBlendDuration);
		tween.TweenProperty(Player.PlayerInstance.Camera, "rotation", Vector3.Zero, CameraBlendDuration);

		Player.PlayerInstance.bDisablePlayerInput = false;
		keypadUiInstance.QueueFree();
		bUsingKeypad = false;
		Input.SetMouseMode(Input.MouseModeEnum.Captured);
	}
}
