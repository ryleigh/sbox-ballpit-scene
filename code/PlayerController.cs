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

	[Sync] public bool IsSpectator { get; set; }

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

		if(IsSpectator)
			Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( Utils.VectorToDegrees(Velocity) ), Velocity.Length * 0.2f * Time.Delta );
		else
			Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( PlayerNum == 0 ? 0f : 180f ), 5f * Time.Delta );

		if ( IsProxy )
			return;

		var wishMoveDir = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x ).Normal;

		Velocity = Utils.DynamicEaseTo( Velocity, wishMoveDir * WalkSpeed, 0.2f, Time.Delta );
		Transform.Position += (Vector3)Velocity * Time.Delta;
		Transform.Position = Transform.Position.WithZ( IsSpectator ? 80f : 0f );

		if ( IsSpectator )
		{
			CheckBoundsSpectator();
		}
		else
		{
			if ( Input.Pressed( "attack1" ) )
			{
				Manager.Instance.SpawnBall( Pos2D + ForwardVec2D * 25f, ForwardVec2D * 100f, PlayerNum, PlayerNum );
			}
			if ( Input.Pressed( "attack2" ) )
			{
				Manager.Instance.SpawnBall( Pos2D + ForwardVec2D * 25f, ForwardVec2D * 100f, OpponentPlayerNum, PlayerNum );
			}

			CheckBoundsPlaying();
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsProxy || IsDead )
			return;

		
	}

	void CheckBoundsPlaying()
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

	void CheckBoundsSpectator()
	{
		var xLeftWall = -245f;
		var xRightWall = -xLeftWall;
		var yBotWall = -131f;
		var yTopWall = -yBotWall;

		var x = Transform.Position.x;
		var y = Transform.Position.y;

		if( x > xLeftWall && x < xRightWall && y > yBotWall && y < yTopWall )
		{
			var xDiff = x < 0f ? x - xLeftWall : xRightWall - x;
			var yDiff = y < 0f ? y - yBotWall : yTopWall - y;

			if(xDiff < yDiff)
			{
				Transform.Position = Transform.Position.WithX( x < 0f ? xLeftWall : xRightWall );
			}
			else
			{
				Transform.Position = Transform.Position.WithY( y < 0f ? yBotWall : yTopWall );
			}
		}

		var xMin = -256f;
		var xMax = -xMin;
		var yMin = -141f;
		var yMax = -yMin;

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
		if ( IsProxy || IsDead || IsSpectator )
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
