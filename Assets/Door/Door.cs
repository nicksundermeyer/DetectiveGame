using Godot;
using System;
using System.Threading.Tasks;

public partial class Door : Interactable
{
	[ExportGroup("Advanced")]
	[Export] public Node3D DoorPivot;
	[Export] public Curve DoorOpenCurve;
	
	public override void Interact()
	{
		_ = OpenDoor();
	}

	private async Task OpenDoor()
	{
		// Smoothly rotate door open
		float dt = (float)GetProcessDeltaTime();
		float moveTime = 2.0f;

		for (float i = 0.0f; i < 1.0f; i+=dt/moveTime)
		{
			Vector3 targetRotation = new Vector3(0.0f, DoorOpenCurve.Sample(i)*90, 0.0f);
			DoorPivot.SetRotationDegrees(targetRotation);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}
}