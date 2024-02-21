using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;
using System.Reflection.Metadata;

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

	[Sync] public int HP { get; set; }
	public int MaxHP { get; set; } = 3;

	protected override void OnAwake()
	{
		base.OnAwake();

		Ragdoll = Components.GetInDescendantsOrSelf<RagdollController>();
		HP = MaxHP;
	}

	public void Respawn()
	{
		if ( IsProxy )
			return;

		Ragdoll.Unragdoll();
		//MoveToSpawnPoint();
		IsDead = false;
		HP = MaxHP;
	}

	protected override void OnUpdate()
	{
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.75f );
		Gizmo.Draw.Text( $"{HP}", new global::Transform( Transform.Position ) );

		if ( IsDead )
		{
			if ( Input.Pressed( "Jump" ) )
			{
				Respawn();
			}

			return;
		}

		Animator.WithVelocity( Velocity * (Velocity.y > 0f ? 0.7f : 0.6f));

		Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( PlayerNum == 0 ? 0f : 180f ), 5f * Time.Delta );

		if ( IsProxy )
			return;

		if( Input.Pressed( "attack1" ) )
		{
			Manager.Instance.SpawnBall( Pos2D + ForwardVec2D * 25f, ForwardVec2D * 100f, PlayerNum, PlayerNum );
		}
		if ( Input.Pressed( "attack2" ) )
		{
			Manager.Instance.SpawnBall( Pos2D + ForwardVec2D * 25f, ForwardVec2D * 100f, OpponentPlayerNum, PlayerNum );
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsDead )
			return;

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
		if ( IsProxy )
			return;

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
				ball.HitPlayer( GameObject.Id );
			}
		}
	}

	public void OnTriggerExit( Collider other )
	{
		if ( IsProxy )
			return;

	}

	public void TakeDamage( Vector2 force )
	{
		if ( IsDead )
			return;

		HP--;

		if ( HP <= 0 )
			Die( force );
	}

	[Broadcast]
	public void Die(Vector3 force)
	{
		Ragdoll.Ragdoll( Transform.Position + Vector3.Up * 100f, force );

		if ( IsProxy )
			return;

		IsDead = true;
	}
}
