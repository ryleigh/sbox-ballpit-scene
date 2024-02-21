using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;

public class PlayerController : Component, Component.ITriggerListener
{
	[Property] public CitizenAnimationHelper Animator { get; set; }
	[Property] public GameObject Model { get; set; }
	public RagdollController Ragdoll { get; private set; }

	[Sync] public int PlayerNum { get; set; } = 0;
	public int OpponentPlayerNum => PlayerNum == 0 ? 1 : 0;

	[Sync] public Vector2 Velocity { get; set; }

	public float WalkSpeed { get; set; } = 95f;
	public Vector2 Pos2D => (Vector2)Transform.Position;
	public Vector2 ForwardVec2D => PlayerNum == 0 ? new Vector2( 1f, 0f ) : new Vector2( -1f, 0f );

	[Sync] public bool IsDead { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Ragdoll = Components.GetInDescendantsOrSelf<RagdollController>();
	}

	//public void Respawn()
	//{
	//	if ( IsProxy )
	//		return;

	//	Ragdoll.Unragdoll();
	//	MoveToSpawnPoint();
	//	LifeState = LifeState.Alive;
	//	Health = MaxHealth;
	//}

	protected override void OnUpdate()
	{
		if ( IsDead )
			return;

		Animator.WithVelocity( Velocity * (Velocity.y > 0f ? 0.7f : 0.6f));

		if ( IsProxy )
			return;

		if( Input.Pressed( "attack1" ) )
		{
			Manager.Instance.SpawnBall( Pos2D + ForwardVec2D * 25f, ForwardVec2D * 100f, PlayerNum );
		}
		if ( Input.Pressed( "attack2" ) )
		{
			Manager.Instance.SpawnBall( Pos2D + ForwardVec2D * 25f, ForwardVec2D * 100f, OpponentPlayerNum );
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsDead )
			return;

		Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( PlayerNum == 0 ? 0f : 180f ), 5f * Time.Delta );

		if ( IsProxy )
			return;

		var wishMoveDir = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x ).Normal;

		Velocity = Utils.DynamicEaseTo( Velocity, wishMoveDir * WalkSpeed, 0.2f, Time.Delta );
		Transform.Position += (Vector3)Velocity * Time.Delta;

		CheckBounds();
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

			if(ball.PlayerNum == PlayerNum)
			{
				ball.Velocity = ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).Normal * ball.Velocity.Length;
			}
			else
			{
				TakeDamage( ball.Velocity * 0.025f );
			}
		}
	}

	public void OnTriggerExit( Collider other )
	{
		
	}

	[Broadcast]
	public void TakeDamage(Vector2 force)
	{
		Die( force );
	}

	public void Die(Vector3 force)
	{
		if ( IsDead )
			return;

		Ragdoll.Ragdoll( Transform.Position + Vector3.Up * 100f, force );

		if ( IsProxy )
			return;

		IsDead = true;
	}
}
