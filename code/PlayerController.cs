using Sandbox;
using Sandbox.Citizen;

public class PlayerController : Component
{
	[Property] public CitizenAnimationHelper Animator { get; set; }
	[Property] public GameObject Model { get; set; }
	public float WalkSpeed { get; set; } = 95f;
	public float RunSpeed { get; set; } = 165f;

	public Vector2 Velocity { get; set; }

	protected override void OnUpdate()
	{
		Animator.WithVelocity( Velocity * 1f );

	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsProxy )
			return;

		var wishMoveDir = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x ).Normal;

		Velocity = Utils.DynamicEaseTo( Velocity, wishMoveDir * (Input.Down( "Run" ) ? RunSpeed : WalkSpeed), (Input.Down( "Run" ) ? 0.06f : 0.25f), Time.Delta );
		Transform.Position += (Vector3)Velocity * Time.Delta;

		var wishYaw = Utils.VectorToDegrees( wishMoveDir );
		Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( wishYaw ), 5f * wishMoveDir.Length * Time.Delta );
	}
}
