using Sandbox;
using System.Numerics;

public class Ball : Component
{
	[Sync] public int PlayerNum { get; set; }
	[Sync] public int CurrentSide { get; set; }

	[Sync] public Vector2 Velocity { get; set; }
	[Property, Sync, Hide] public Color Color { get; set; }

	//public HighlightOutline HighlightOutline { get; private set; }
	public ModelRenderer ModelRenderer { get; private set; }

	[Sync] public bool IsActive { get; set; }

	public bool IsDespawning { get; private set; }
	public TimeSince TimeSinceDespawnStart { get; private set; }
	private float _despawnTime = 3f;
	private bool _moveWhileDespawning = true;

	public bool IsWobbling { get; set; }
	public TimeSince TimeSinceWobble { get; private set; }
	private float _wobbleTime = 0.5f;

	public TimeSince TimeSinceBumped { get; private set; }

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

		//HighlightOutline = Components.Get<HighlightOutline>();
		ModelRenderer = Components.Get<ModelRenderer>();
	}

	protected override void OnStart()
	{
		base.OnStart();

		IsActive = true;
		TimeScale = 1f;

		_localScaleStart = Transform.LocalScale;

		if ( IsProxy )
			return;

		//Velocity = (new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) )).Normal * 100f;
	}

	public void SetRadius( float radius)
	{
		Radius = radius;
		var scale = radius * (0.25f / 8f);
		Transform.LocalScale = _localScaleStart = new Vector3( scale );
	}

	protected override void OnUpdate()
	{
		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"{Velocity.Length}", new global::Transform( Transform.Position + new Vector3(0f, 1f, 1f)) );

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
			// don't move if IsProxy?
			if(!IsProxy)
				Transform.Position += (Vector3)Velocity * Time.Delta * TimeScale;

			// todo: change height when changing ownership
			var height = (PlayerNum == 0 && Transform.Position.x > Manager.Instance.CenterLineOffset || PlayerNum == 1 && Transform.Position.x < Manager.Instance.CenterLineOffset) ? Manager.BALL_HEIGHT_OPPONENT : Manager.BALL_HEIGHT_SELF;
			Transform.Position = Transform.Position.WithZ( height );

			if ( ModelRenderer != null )
				ModelRenderer.Tint = Color;

			//if ( ModelRenderer != null )
			//	ModelRenderer.Tint = Color.WithAlpha( Utils.Map( Utils.FastSin( PlayerNum * 16f + Time.Now * 8f ), -1f, 1f, 0.8f, 1.2f, EasingType.SineInOut ) );
		}

		//if(HighlightOutline != null)
		//	HighlightOutline.Width = 0.2f + Utils.FastSin(Time.Now * 16f) * 0.05f;

		if ( IsProxy )
			return;

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
		var xMax = Manager.X_FAR + (PlayerNum == 1 ? 10f : 0f);
		var yMin = -Manager.Y_LIMIT - 8f;
		var yMax = Manager.Y_LIMIT + 8f;

		if ( Transform.Position.x < xMin )
		{
			if ( PlayerNum == 0 )
			{
				EnterGutter();
				return;
			}
			else
			{
				HitBarrier();
				Transform.Position = Transform.Position.WithX( xMin );
				Velocity = Velocity.WithX( MathF.Abs( Velocity.x ) );
			}
		}
		else if ( Transform.Position.x > xMax )
		{
			if ( PlayerNum == 0 )
			{
				HitBarrier();
				Transform.Position = Transform.Position.WithX( xMax );
				Velocity = Velocity.WithX( -MathF.Abs( Velocity.x ) );
			}
			else
			{
				EnterGutter();
				return;
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

		//if ( IsProxy )
		//	return;

		PlayerNum = playerNum;
		
		//Log.Info( $"Color: {Color} Manager.Instance.ColorPlayer0: {Manager.Instance.ColorPlayer0} Manager.Instance.ColorPlayer1: {Manager.Instance.ColorPlayer1} IsProxy: {IsProxy}" );

		//Components.Get<ModelRenderer>().Tint = Color;

		//var highlightOutline = Components.Get<HighlightOutline>();
		//highlightOutline.Width = 0.2f;
		//highlightOutline.Color = Color;
		//highlightOutline.InsideColor = Color.WithAlpha( 0.75f );
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
		if ( !IsActive )
			return;

		Manager.Instance.CreateBallExplosionParticles( Transform.Position, PlayerNum );

		IsDespawning = true;
		TimeSinceDespawnStart = 0f;
		_despawnTime = 0.1f;
		_moveWhileDespawning = false;

		if ( IsProxy )
			return;

		//GameObject.Destroy();

		IsActive = false;
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
	public void HitBarrier()
	{
		Sound.Play( "frame-bounce", Transform.Position.WithZ(Globals.SFX_HEIGHT) );
	}

	[Broadcast]
	public void Despawn()
	{
		if(!IsActive)
			return;

		IsDespawning = true;
		TimeSinceDespawnStart = 0f;

		if ( IsProxy )
			return;

		IsActive = false;
	}

	//[Broadcast]
	//public void DestroyBall()
	//{
	//	GameObject.Destroy();
	//}

	[Broadcast]
	public void BumpedByPlayer()
	{
		IsWobbling = true;
		TimeSinceWobble = 0f;
		TimeSinceBumped = 0f;
	}

	[Broadcast]
	public void SetDirection(Vector2 dir)
	{
		if ( IsProxy )
			return;

		Velocity = dir * Velocity.Length;
	}

	[Broadcast]
	public void SetVelocity( Vector2 velocity, float timeScale = 1f, float duration = 0f, EasingType easingType = EasingType.Linear )
	{
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
		if ( IsDespawning || !IsActive || (_timeScaleActive && TimeScale < timeScale) )
			return;

		_timeScaleActive = true;
		TimeScale = timeScale;
		_timeScaleStartingValue = timeScale;
		_timeScaleDuration = time;
		_timeScaleEasingType = easingType;
		_timeSinceTimeScaleStarted = 0f;
	}
}
