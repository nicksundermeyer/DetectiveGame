using Godot;
using System;
using System.Collections;
using System.Threading.Tasks;

namespace Godot.Collections;

[GlobalClass]
public partial class Keypad : Interactable
{
	[Export] public int Code = 1234;
	[Export] public Interactable ActivateObject;
	
	[ExportGroup("Advanced")]
	[Export] public Camera3D Camera;
	[Export] public Node3D KeypadMeshes;
	[Export] public Label3D ScreenText;
	[Export] public PackedScene KeypadUi;
	[Export] public Curve ButtonPressCurve;
	[Export] public float CameraBlendDuration = 0.5f;

	private Array<Node> buttonMeshes = [];
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
	
	// Called when any input event occurs (UI, mouse, keyboard, etc.)
	public override void _UnhandledInput(InputEvent @event)
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
						_ = PressButton(buttonMeshes.IndexOf(hitCollider.GetParent()));
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
	
	private async Task PressButton(int buttonIndex)
	{
		bool bFailure = false;
		bool bSuccess = false;
		if (buttonIndex == 10)
		{
			// Clear button
			ScreenText.SetText("");
			ScreenText.Modulate = Colors.White;
		}
		else if (buttonIndex == 11)
		{
			// Submit button
			if (ScreenText.GetText() == Code.ToString())
			{
				ScreenText.Modulate = Colors.Green;
				bSuccess = true;
			}
			else
			{
				bFailure = true;
				ScreenText.Modulate = Colors.Red;
			}
		}
		else
		{
			// Type text into screen, clamped to password length
			if (ScreenText.GetText().Length < 4)
			{
				ScreenText.SetText(ScreenText.GetText() + buttonIndex);
			}
		}
		
		// Move button
		float dt = (float)GetProcessDeltaTime();
		float moveTime = 0.2f;
		float moveDistance = 0.01f;
		Node3D buttonMesh = (Node3D)buttonMeshes[buttonIndex];
		Vector3 start = buttonMesh.GetPosition();

		for (float i = 0.0f; i < 1.0f; i+=dt/moveTime)
		{
			Vector3 targetPosition = new Vector3(start.X, start.Y, start.Z - ButtonPressCurve.Sample(i)*moveDistance);
			buttonMesh.SetPosition(targetPosition);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		// Wait a moment and then clear or exit the text if needed
		if (bFailure || bSuccess)
		{
			await ToSignal(GetTree().CreateTimer(1.0f), SceneTreeTimer.SignalName.Timeout);
		}

		if (bFailure)
		{
			ScreenText.SetText("");
			ScreenText.Modulate = Colors.White;
		}

		if (bSuccess)
		{
			ExitKeypad();
			if (ActivateObject != null)
			{
				ActivateObject.Interact();
			}
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
