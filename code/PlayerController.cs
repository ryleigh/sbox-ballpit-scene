using Sandbox;
using Sandbox.Citizen;

public class PlayerController : Component, Component.ITriggerListener
{
	[Property] public CitizenAnimationHelper Animator { get; set; }
	[Property] public GameObject Model { get; set; }

	[Sync] public int PlayerNum { get; set; } = 0;

	[Sync] public Vector2 Velocity { get; set; }
	public float WalkSpeed { get; set; } = 95f;

	protected override void OnUpdate()
	{
		Animator.WithVelocity( Velocity * (Velocity.y > 0f ? 0.7f : 0.6f));
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsProxy )
			return;

		var wishMoveDir = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x ).Normal;

		Velocity = Utils.DynamicEaseTo( Velocity, wishMoveDir * WalkSpeed, 0.2f, Time.Delta );
		Transform.Position += (Vector3)Velocity * Time.Delta;

		CheckBounds();

		var wishYaw = PlayerNum == 0 ? 0f : 180f;
		Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( wishYaw ), 5f * wishMoveDir.Length * Time.Delta );
	}

	void CheckBounds()
	{
		var xMin = PlayerNum == 0 ? -Manager.X_FAR : Manager.X_CLOSE;
		var xMax = PlayerNum == 0 ? -Manager.X_CLOSE : Manager.X_FAR;
		var yMin = -Manager.Y_LIMIT;
		var yMax = Manager.Y_LIMIT;

		if ( Transform.Position.x < xMin )
			Transform.Position = Transform.Position.WithX( xMin );
		else if ( Transform.Position.x > xMax )
			Transform.Position = Transform.Position.WithX( xMax );

		if ( Transform.Position.y < yMin )
			Transform.Position = Transform.Position.WithY( yMin );
		else if ( Transform.Position.y > yMax )
			Transform.Position = Transform.Position.WithY( yMax );
	}

	public void OnTriggerEnter( Collider other )
	{
		if(other.GameObject.Tags.Has("ball"))
		{
			var ball = other.Components.Get<Ball>();
			ball.Velocity = ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).Normal * ball.Velocity.Length;
		}
	}

	public void OnTriggerExit( Collider other )
	{
		
	}
}
