using Sandbox;

public class Ball : Component
{
	[Sync] public int PlayerNum { get; set; }
	[Sync] public int CurrentSide { get; set; }

	[Sync] public Vector2 Velocity { get; set; }
	[Property, Sync, Hide] public Color Color { get; set; }

	public ModelRenderer ModelRenderer { get; private set; }

	public bool IsDespawning { get; private set; }
	public TimeSince TimeSinceDespawnStart { get; private set; }
	private float _despawnTime = 3f;
	private bool _moveWhileDespawning = true;

	public bool IsWobbling { get; set; }
	public TimeSince TimeSinceWobble { get; private set; }
	private float _wobbleTime = 0.5f;

	public TimeSince TimeSinceBumped { get; set; }

	private Vector3 _localScaleStart;

	[Sync] public float Radius { get; private set; }

	private bool _timeScaleActive;
	private float TimeScale;
	private float _timeScaleDuration;
	private EasingType _timeScaleEasingType;
	private TimeSince _timeSinceTimeScaleStarted;
	private float _timeScaleStartingValue;

	public const float MAX_SPEED = 320f;

	protected override void OnAwake()
	{
		base.OnAwake();

		ModelRenderer = Components.Get<ModelRenderer>();
	}

	protected override void OnStart()
	{
		base.OnStart();

		TimeScale = 1f;

		_localScaleStart = Transform.LocalScale;

		if ( IsProxy )
			return;
	}

	public void Init( int playerNum, int currentSide, float radius, Vector2 velocity )
	{
		PlayerNum = playerNum;
		CurrentSide = currentSide;

		Radius = radius;
		var scale = radius * (0.25f / 8f);
		Transform.LocalScale = _localScaleStart = new Vector3( scale );

		Velocity = velocity;
	}

	protected override void OnUpdate()
	{
		//if(IsDespawning)
		//{
		//	Gizmo.Draw.Color = Color.White;
		//	Gizmo.Draw.Text( $"IsDespawning: {IsDespawning}", new global::Transform( Transform.Position + new Vector3( 0f, 1f, 1f ) ) );
		//}

		if(IsWobbling)
		{
			if(TimeSinceWobble >  _wobbleTime)
			{
				IsWobbling = false;
				Transform.LocalScale = _localScaleStart;
			}
			else
			{
				float amount = Utils.Map( TimeSinceWobble, 0f, _wobbleTime, 0.1f, 0f, EasingType.QuadOut );
				var targetScale = new Vector3( _localScaleStart.x + Game.Random.Float( -amount, amount ), _localScaleStart.y + Game.Random.Float( -amount, amount ), _localScaleStart.z + Game.Random.Float( -amount, amount ) );
				Transform.LocalScale = Vector3.Lerp( Transform.LocalScale, targetScale, Time.Delta * 100f );
			}
		}

		if ( IsDespawning)
		{
			if( _moveWhileDespawning )
			{
				Velocity *= (1f - 3f * Time.Delta);
				Transform.Position += (Vector3)Velocity * Time.Delta;
			}

			if ( ModelRenderer != null )
				ModelRenderer.Tint = Color.Lerp( Color, Color.WithAlpha(0f), Utils.Map(TimeSinceDespawnStart, 0f, _despawnTime, 0f, 1f) );
		}
		else
		{
			if(!IsProxy) // todo: still move even if proxy?
				Transform.Position += (Vector3)Velocity * Time.Delta * TimeScale;

			// todo: change height when changing ownership instead of every frame
			var height = (PlayerNum == 0 && Transform.Position.x > Manager.Instance.CenterLineOffset || PlayerNum == 1 && Transform.Position.x < Manager.Instance.CenterLineOffset) ? Manager.BALL_HEIGHT_OPPONENT : Manager.BALL_HEIGHT_SELF;
			Transform.Position = Transform.Position.WithZ( height );

			if ( ModelRenderer != null )
				ModelRenderer.Tint = Color; // todo: don't do every frame
		}

		if ( IsProxy )
			return;

		//SetRadius( Radius + 5f * Time.Delta );

		if(_timeScaleActive)
		{
			if ( _timeSinceTimeScaleStarted > _timeScaleDuration )
			{
				TimeScale = 1f;
				_timeScaleActive = false;
			}
			else 
			{
				TimeScale = Utils.Map( _timeSinceTimeScaleStarted, 0f, _timeScaleDuration, _timeScaleStartingValue, 1f, _timeScaleEasingType );
			}
		}

		if ( IsDespawning )
		{
			if ( TimeSinceDespawnStart > _despawnTime )
			{
				GameObject.Destroy();
				return;
			}
		}

		CheckBounds();

		if ( CurrentSide == 0 && Transform.Position.x > Manager.Instance.CenterLineOffset )
			SetSide( 1 );
		else if ( CurrentSide == 1 && Transform.Position.x < Manager.Instance.CenterLineOffset )
			SetSide( 0 );
	}

	void CheckBounds()
	{
		var xMin = -Manager.X_FAR + (PlayerNum == 0 ? -10f : 0f);
		var xMinBarrier = -Manager.X_FAR + 5f;
		var xMax = Manager.X_FAR + (PlayerNum == 1 ? 10f : 0f);
		var xMaxBarrier = Manager.X_FAR - 5f;
		var yMin = -Manager.Y_LIMIT - 8f;
		var yMax = Manager.Y_LIMIT + 8f;

		// barrier
		if( Transform.Position.x < xMinBarrier && PlayerNum == 0 )
		{
			var leftBarrierActive = Manager.Instance.GetPlayer( 0 )?.IsBarrierActive ?? false;
			if ( leftBarrierActive )
			{
				HitSideGutterBarrier( left: true );
				Transform.Position = Transform.Position.WithX( xMinBarrier );
				Velocity = Velocity.WithX( MathF.Abs( Velocity.x ) );
				return;
			}
		}
		else if( Transform.Position.x > xMaxBarrier && PlayerNum == 1 )
		{
			var rightBarrierActive = Manager.Instance.GetPlayer( 1 )?.IsBarrierActive ?? false;
			if ( rightBarrierActive )
			{
				HitSideGutterBarrier( left: false );
				Transform.Position = Transform.Position.WithX( xMaxBarrier );
				Velocity = Velocity.WithX( -MathF.Abs( Velocity.x ) );
				return;
			}
		}

		// wall & gutter
		if ( Transform.Position.x < xMin )
		{
			if ( PlayerNum == 0 )
			{
				EnterGutter();
				return;
			}
			else
			{
				HitSideWall(left: true);
				Transform.Position = Transform.Position.WithX( xMin );
				Velocity = Velocity.WithX( MathF.Abs( Velocity.x ) );
			}
		}
		else if ( Transform.Position.x > xMax )
		{
			if ( PlayerNum == 1 )
			{
				EnterGutter();
				return;
			}
			else
			{
				HitSideWall(left: false);
				Transform.Position = Transform.Position.WithX( xMax );
				Velocity = Velocity.WithX( -MathF.Abs( Velocity.x ) );
			}
		}

		if ( Transform.Position.y < yMin )
		{
			Velocity = Velocity.WithY( MathF.Abs( Velocity.y) );
		}
		else if ( Transform.Position.y > yMax )
		{
			Velocity = Velocity.WithY( -MathF.Abs( Velocity.y ) );
		}
	}

	[Broadcast]
	public void SetPlayerNum( int playerNum )
	{
		Color = (playerNum == 0 ? Manager.Instance.ColorPlayer0 : Manager.Instance.ColorPlayer1);

		PlayerNum = playerNum;
	}

	public void SetSide( int side )
	{
		if ( CurrentSide == side )
			return;

		var connection = Manager.Instance.GetConnection( side );
		//Log.Info( $"{GameObject.Name} isHost: {(Network.OwnerConnection?.IsHost.ToString() ?? "...")} isOwner: {Network.IsOwner} SetSide: {CurrentSide}->{side} curr: {Network.OwnerConnection?.Id.ToString().Substring( 0, 6 ) ?? "..."} , switching to: {connection?.Id.ToString().Substring( 0, 6 ) ?? "..."}" );
		
		CurrentSide = side;

		if ( connection != null )
			Network.AssignOwnership( connection );
	}

	[Broadcast]
	public void HitPlayer(Guid hitPlayerId)
	{
		if ( IsDespawning )
			return;

		Manager.Instance.CreateBallExplosionParticles( Transform.Position, PlayerNum );

		IsDespawning = true;
		TimeSinceDespawnStart = 0f;
		_despawnTime = 0.1f;
		_moveWhileDespawning = false;
	}

	[Broadcast]
	public void EnterGutter()
	{
		Sound.Play( "claw-firework-explode", Transform.Position.WithZ(Globals.SFX_HEIGHT) );

		Manager.Instance.CreateBallGutterParticles( Transform.Position, PlayerNum );

		if ( IsProxy )
			return;

		GameObject.Destroy();
	}

	[Broadcast]
	public void HitSideWall(bool left)
	{
		Sound.Play( "frame-bounce", Transform.Position.WithZ(Globals.SFX_HEIGHT) );

		if ( left )
			Manager.Instance.TimeSinceLeftWallRebound = 0f;
		else
			Manager.Instance.TimeSinceRightWallRebound = 0f;

		if ( IsProxy )
			return;

		var player = Manager.Instance.GetPlayer( PlayerNum );
		if ( player == null )
			return;

		var backstabChance = player.GetUpgradeLevel( UpgradeType.Backstab ) > 0f ? BackstabUpgrade.GetChance( player.GetUpgradeLevel( UpgradeType.Backstab ) ) : 0f;
		if (Game.Random.Float(0f, 1f) < backstabChance )
		{
			var enemy = Manager.Instance.GetPlayer(Globals.GetOpponentPlayerNum(PlayerNum));
			Vector2 enemyPos = enemy != null ? (Vector2)enemy.Transform.Position : Vector2.Zero;
			var dir = (enemyPos - (Vector2)Transform.Position).Normal;
			SetVelocity( dir * Velocity.Length, timeScale: 0f, duration: 0.5f, EasingType.QuadIn );
		}
	}

	[Broadcast]
	public void HitSideGutterBarrier( bool left )
	{
		Sound.Play( "barrier_impact", Transform.Position.WithZ( Globals.SFX_HEIGHT ) );

		if ( left )
			Manager.Instance.TimeSinceLeftGutterBarrierRebound = 0f;
		else
			Manager.Instance.TimeSinceRightGutterBarrierRebound = 0f;
	}

	[Broadcast]
	public void Despawn()
	{
		if(IsDespawning)
			return;

		IsDespawning = true;
		TimeSinceDespawnStart = 0f;
		_moveWhileDespawning = true;
	}

	[Broadcast]
	public void BumpedByPlayer()
	{
		IsWobbling = true;
		TimeSinceWobble = 0f;
		//TimeSinceBumped = 0f;
	}

	[Broadcast]
	public void SetDirection(Vector2 dir)
	{
		if ( IsProxy )
			return;

		Velocity = dir * Velocity.Length;
	}

	[Broadcast]
	public void SetVelocity( Vector2 velocity, float timeScale = 1f, float duration = 0f, EasingType easingType = EasingType.Linear, bool showArrow = true )
	{
		if(showArrow)
		{
			var ballPos = (Vector2)Transform.Position;
			Manager.Instance.DisplayArrow(
				pos: ballPos,
				dir: velocity.Normal,
				lifetime: Game.Random.Float( 0.45f, 0.55f ),
				speed: Game.Random.Float( 85f, 95f ),
				deceleration: Game.Random.Float( 2.6f, 3.4f ),
				color: PlayerNum == 0 ? new Color( 0.6f, 0.6f, 1f ) : new Color( 0.4f, 1f, 0.4f )
			);
		}

		if ( IsProxy )
			return;

		Velocity = velocity;

		if ( timeScale < 1f && duration > 0f )
			SetTimeScale( timeScale, duration, easingType );
	}

	[Broadcast]
	public void SetTimeScaleRPC( float timeScale, float duration, EasingType easingType = EasingType.Linear )
	{
		if ( IsProxy )
			return;

		SetTimeScale( timeScale, duration, easingType );
	}

	public void SetTimeScale( float timeScale, float time, EasingType easingType = EasingType.Linear )
	{
		if ( IsDespawning || (_timeScaleActive && TimeScale < timeScale) )
			return;

		_timeScaleActive = true;
		TimeScale = timeScale;
		_timeScaleStartingValue = timeScale;
		_timeScaleDuration = time;
		_timeScaleEasingType = easingType;
		_timeSinceTimeScaleStarted = 0f;
	}
}
