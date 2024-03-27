using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;
using System.Numerics;
using System.Reflection.Metadata;

public class PlayerController : Component, Component.ITriggerListener
{
	[Property] public CitizenAnimationHelper Animator { get; set; }
	[Property] public GameObject Model { get; set; }
	//[Property] public GameObject InnerHitbox { get; set; }
	public RagdollController Ragdoll { get; private set; }

	[Sync] public int PlayerNum { get; set; } = 0;
	public int OpponentPlayerNum => PlayerNum == 0 ? 1 : 0;

	[Sync] public Vector2 Velocity { get; set; }

	public Vector2 Pos2D => (Vector2)Transform.Position;
	public Vector2 ForwardVec2D => PlayerNum == 0 ? new Vector2( 1f, 0f ) : new Vector2( -1f, 0f );

	[Sync] public bool IsDead { get; set; }
	[Sync] public bool IsJumping { get; set; }
	private Vector3 _jumpStartPos;
	private Vector3 _jumpTargetPos;
	private TimeSince _timeSinceJump;
	private float _jumpTime;

	[Sync] public int HP { get; set; }
	public int MaxHP { get; set; } = 3;

	[Sync] public bool IsSpectator { get; set; }
	[Sync] public int Money { get; private set; }
	[Sync] public int NumMatchWins { get; private set; }
	[Sync] public int NumMatchLosses { get; private set; }
	[Sync] public int NumShopItems { get; private set; }

	[Sync] public NetDictionary<UpgradeType, int> PassiveUpgrades { get; set; } = new();
	[Sync] public NetDictionary<UpgradeType, int> ActiveUpgrades { get; set; } = new();
	public const int MAX_UPGRADE_LEVEL = 10;

	[Sync] public UpgradeType SelectedUpgradeType { get; set; }

	private bool _isFlashing;
	private TimeSince _timeSinceFlashToggle;
	private bool _renderersVisible;

	[Sync] public bool IsInvulnerable { get; set; }
	private TimeSince _timeSinceInvulnerableStart;
	public float InvulnerableTime { get; set; } = 1f;

	public float RadiusLarge { get; set; } = 6f;
	public float RadiusSmall { get; set; } = 1.2f;

	protected override void OnAwake()
	{
		base.OnAwake();

		Ragdoll = Components.GetInDescendantsOrSelf<RagdollController>();
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
			return;

	}

	protected override void OnUpdate()
	{
		//using ( Gizmo.Scope() )
		//{
		//	Gizmo.Draw.Color = Color.Red;
		//	Gizmo.Draw.LineSphere( Transform.Position.WithZ(100f), RadiusSmall );

		//	Gizmo.Draw.Color = Color.Blue;
		//	Gizmo.Draw.LineSphere( Transform.Position.WithZ( 100f ), RadiusLarge );

		//	foreach ( var ball in Scene.GetAllComponents<Ball>() )
		//	{
		//		Gizmo.Draw.Color = Color.White;
		//		Gizmo.Draw.LineSphere( ball.Transform.Position.WithZ( 100f ), ball.Radius );
		//	}
		//}

		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.95f );
		//Gizmo.Draw.Text( $"{NumMatchWins} - {NumMatchLosses}", new global::Transform( Transform.Position ) );

		Animator.WithVelocity( Velocity * (Velocity.y > 0f ? 0.7f : 0.6f));

		if(!IsJumping)
		{
			if ( IsSpectator )
				Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( Utils.VectorToDegrees( Velocity ) ), Velocity.Length * 0.2f * Time.Delta );
			else
				Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( PlayerNum == 0 ? 0f : 180f ), 5f * Time.Delta );
		}

		if( _isFlashing && _timeSinceFlashToggle > 0.04f )
		{
			SetRendererVisibility( !_renderersVisible );
			_renderersVisible = !_renderersVisible;
			_timeSinceFlashToggle = 0f;
		}

		if ( IsProxy )
			return;

		if(IsInvulnerable && _timeSinceInvulnerableStart > InvulnerableTime)
		{
			EndInvulnerability();
		}

		if(IsJumping)
		{
			if(_timeSinceJump > _jumpTime)
			{
				IsJumping = false;
				Transform.Position = _jumpTargetPos;
				Model.Transform.LocalScale = Vector3.One;
			}
			else
			{
				var progress = Utils.Map( _timeSinceJump, 0f, _jumpTime, 0f, 1f, EasingType.QuadInOut );
				Transform.Position = Vector3.Lerp( _jumpStartPos, _jumpTargetPos, progress );
				Model.Transform.LocalScale = Vector3.One * Utils.MapReturn(progress, 0f, 1f, 1f, 1.6f, EasingType.Linear);
			}

			return;
		}

		if ( IsDead )
			return;

		var wishMoveDir = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x ).Normal;

		float moveSpeed = Utils.Map( GetUpgradeLevel( UpgradeType.MoveSpeed ), 0, 10, 90f, 125f, EasingType.SineOut );
		Velocity = Utils.DynamicEaseTo( Velocity, wishMoveDir * moveSpeed, 0.2f, Time.Delta );
		Transform.Position += (Vector3)Velocity * Time.Delta;
		Transform.Position = Transform.Position.WithZ( IsSpectator ? Manager.SPECTATOR_HEIGHT : 0f );

		if ( IsSpectator )
		{
			CheckCollisionWithPlayers();
			Transform.Position = GetClosestSpectatorPos( Transform.Position );
		}
		else
		{
			if ( Input.Pressed( "Jump" ) )
			{
				TryUseItem(SelectedUpgradeType);
			}
			else if(Input.MouseWheel != Vector2.Zero)
			{
				ChangeSelectedActiveUpgrade( up: Input.MouseWheel.y > 0f );
			}

			CheckBoundsPlaying();

			// collide with balls
			if ( Manager.Instance.GamePhase == GamePhase.RoundActive )
			{
				foreach ( var ball in Scene.GetAllComponents<Ball>() )
				{
					if ( !ball.IsActive )
						continue;

					if ( ball.PlayerNum == PlayerNum )
					{
						if ( ball.TimeSinceBumped > 0.5f &&
							(ball.Transform.Position - Transform.Position).WithZ( 0f ).LengthSquared < MathF.Pow( RadiusLarge + ball.Radius, 2f ) )
						{
							var direction = ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).Normal;
							ball.Velocity = direction * ball.Velocity.Length;
							ball.BumpedByPlayer();
							BumpOwnBall( (Vector2)ball.Transform.Position );
						}
					}
					else
					{
						if ( !IsInvulnerable &&
							 (ball.Transform.Position - Transform.Position).WithZ( 0f ).LengthSquared < MathF.Pow( RadiusSmall + ball.Radius, 2f ) )
						{
							HitOpponentBall( ball );
						}
					}
				}
			}
		}
	}

	void ChangeSelectedActiveUpgrade(bool up)
	{
		if ( ActiveUpgrades.Count < 2 )
			return;

		var upgradeTypes = ActiveUpgrades.Keys;
		var selectedIndex = 0;
		int count = 0;
		foreach ( var upgradeType in upgradeTypes ) 
		{ 
			if( upgradeType == SelectedUpgradeType)
			{
				selectedIndex = count;
				break;
			}

			count++;
		}

		int nextIndex = up
			? (selectedIndex < ActiveUpgrades.Count - 1 ? selectedIndex + 1 : 0)
			: (selectedIndex > 0 ? selectedIndex - 1 : upgradeTypes.Count - 1);

		SelectedUpgradeType = upgradeTypes.ElementAt(nextIndex);
	}

	public void TryUseItem(UpgradeType upgradeType)
	{
		if ( Manager.Instance.GamePhase != GamePhase.RoundActive ||
			!ActiveUpgrades.ContainsKey( upgradeType ) )
			return;

		switch( upgradeType )
		{
			case UpgradeType.None: default:
				break;
			case UpgradeType.Volley:
				var currDegrees = -30f;
				for(int i = 0; i < 5; i++)
				{
					var forwardDegrees = Utils.VectorToDegrees( ForwardVec2D );
					var vec = Utils.DegreesToVector( currDegrees + forwardDegrees );
					var speed = 85f;
					Manager.Instance.SpawnBall( Pos2D + vec * 25f, vec * speed, PlayerNum );
					currDegrees += 15f;
				}

				break;
			case UpgradeType.Gather:
				foreach ( var ball in Scene.GetAllComponents<Ball>() )
				{
					if ( ball.IsActive && ball.PlayerNum == PlayerNum )
					{
						var speed = ball.Velocity.Length;
						var dir = ((Vector2)Transform.Position - (Vector2)ball.Transform.Position).Normal;
						ball.SetVelocity( dir * speed, timeScale: 0f, duration: 0.2f, EasingType.Linear );
					}
				}

				break;
			case UpgradeType.Repel:
				foreach ( var ball in Scene.GetAllComponents<Ball>() )
				{
					if(!ball.IsActive)
						continue;

					var distSqr = ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).LengthSquared;
					if(distSqr < MathF.Pow(100f, 2f))
					{
						var speed = ball.Velocity.Length * 1.15f;
						var dir = ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).Normal;
						ball.SetVelocity( dir * speed, timeScale: 0f, duration: 0.1f, EasingType.ExpoIn );
					}
				}

				break;
			case UpgradeType.Replace:
				foreach ( var ball in Scene.GetAllComponents<Ball>() )
				{
					if ( !ball.IsActive )
						continue;

					ball.SetTimeScaleRPC( timeScale: 0f, duration: 0.75f, EasingType.QuadIn );
					ball.SetPlayerNum( Globals.GetOpponentPlayerNum( ball.PlayerNum ) );
				}

				break;
		}

		if ( upgradeType != UpgradeType.None )
			AdjustUpgradeLevel( upgradeType, -1 );

		if ( Manager.Instance.HoveredUpgradeType != UpgradeType.None && !ActiveUpgrades.ContainsKey( Manager.Instance.HoveredUpgradeType ) )
			Manager.Instance.HoveredUpgradeType = UpgradeType.None;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsProxy || IsDead )
			return;

		
	}

	void CheckBoundsPlaying()
	{
		var center = Manager.Instance.CenterLineOffset;

		var xMin = PlayerNum == 0 ? -Manager.X_FAR : (center + Manager.X_CLOSE);
		var xMax = PlayerNum == 0 ? (center - Manager.X_CLOSE) : Manager.X_FAR;
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

	public Vector3 GetClosestSpectatorPos(Vector3 pos)
	{
		var xLeftWall = -245f;
		var xRightWall = -xLeftWall;
		var yBotWall = -131f;
		var yTopWall = -yBotWall;

		if ( pos.x > xLeftWall && pos.x < xRightWall && pos.y > yBotWall && pos.y < yTopWall )
		{
			var xDiff = pos.x < 0f ? pos.x - xLeftWall : xRightWall - pos.x;
			var yDiff = pos.y < 0f ? pos.y - yBotWall : yTopWall - pos.y;

			if ( xDiff < yDiff )
			{
				pos = pos.WithX( pos.x < 0f ? xLeftWall : xRightWall );
			}
			else
			{
				pos = pos.WithY( pos.y < 0f ? yBotWall : yTopWall );
			}
		}

		var xMin = -256f;
		var xMax = -xMin;
		var yMin = -141f;
		var yMax = -yMin;

		if ( pos.x < xMin )
			pos = pos.WithX( xMin );
		else if ( pos.x > xMax )
			pos = pos.WithX(xMax);

		if ( pos.y < yMin )
			pos = pos.WithY(yMin);
		else if ( pos.y > yMax )
			pos = pos.WithY(yMax);

		return pos;
	}

	void CheckCollisionWithPlayers()
	{
		foreach ( var player in Scene.GetAllComponents<PlayerController>() )
		{
			if ( player == this )
				continue;

			var radius = player.Components.Get<CapsuleCollider>().Radius;
			if((player.Transform.Position - Transform.Position).LengthSquared < MathF.Pow(radius * 2f, 2f))
			{
				Vector3 dir = (Transform.Position - player.Transform.Position).Normal.WithZ( 0f );
				float percent = Utils.Map( (player.Transform.Position - Transform.Position).Length, 0f, radius * 2f, 1f, 0f );

				Transform.Position += dir * percent * 500f * Time.Delta;
			}
		}
	}

	[Broadcast]
	public void BumpOwnBall( Vector2 pos )
	{
		Sound.Play( "impact-thump", new Vector3(pos.x, pos.y, Globals.SFX_HEIGHT ) );
	}

	public void HitOpponentBall( Ball ball )
	{
		ball.HitPlayer( GameObject.Id );
		TakeDamage( ball.Velocity * 0.025f );
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy || IsDead || IsSpectator )
			return;

		//if(other.GameObject.Tags.Has("ball") && Manager.Instance.GamePhase == GamePhase.RoundActive )
		//{
		//	//Log.Info( $"OnTriggerEnter {other.GameObject.Name} time: {Time.Now}" );

		//	var ball = other.Components.Get<Ball>();
		//	if (ball.IsActive && ball.PlayerNum == PlayerNum)
		//	{
		//		ball.BumpedByPlayer( direction: ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).Normal );
		//		HitOwnBall( (Vector2)ball.Transform.Position );
		//	}
		//}

		if(other.GameObject.Tags.Has("item") && Manager.Instance.GamePhase == GamePhase.BuyPhase && Manager.Instance.TimeSincePhaseChange > 0.5f)
		{
			var item = other.Components.Get<ShopItem>();
			if(item.Price <= Money)
			{
				Money -= item.Price;
				AdjustUpgradeLevel( item.UpgradeType, item.NumLevels );

				BuyItem(success: true);

				item.GameObject.Destroy();
			}
			else
			{
				BuyItem( success: false );
			}
		}
		else if ( other.GameObject.Tags.Has( "skip_button" ) && Manager.Instance.GamePhase == GamePhase.BuyPhase && Manager.Instance.TimeSincePhaseChange > 0.5f )
		{
			other.GameObject.Destroy();
			Manager.Instance.SkipButtonHit();

			HitSkipButton();
		}
	}

	public void OnTriggerExit( Collider other )
	{
		//Log.Info( $"OnTriggerExit {other.GameObject.Name} time: {Time.Now}" );

		if ( IsProxy )
			return;

	}

	[Broadcast]
	public void TakeDamage( Vector2 force )
	{
		if ( IsDead )
			return;

		if(HP <= 1)
		{
			Sound.Play( "die", Transform.Position.WithZ( Globals.SFX_HEIGHT ) );
		}
		else
		{
			Sound.Play( "hurt", Transform.Position.WithZ( Globals.SFX_HEIGHT ) );
		}

		if (IsProxy)
			return;

		HP--;

		if ( HP <= 0 )
		{
			Die( force );
		}
		else
		{
			Manager.Instance.PlayerHit(GameObject.Id);
			StartInvulnerability();
		}
	}

	[Broadcast]
	public void Die(Vector3 force)
	{
		if ( IsProxy )
			return;

		if(IsInvulnerable)
			EndInvulnerability();

		Ragdoll.Ragdoll( Transform.Position + Vector3.Up * 100f, force );

		IsDead = true;
		Velocity = Vector2.Zero;

		Manager.Instance.PlayerDied( GameObject.Id );
	}

	[Broadcast]
	public void Respawn()
	{
		_isFlashing = false;
		SetRendererVisibility( visible: true );

		if ( IsProxy )
			return;

		if(IsDead)
		{
			Ragdoll.Unragdoll();
			//MoveToSpawnPoint();
			IsDead = false;
		}
		
		HP = MaxHP;
		IsInvulnerable = false;
	}

	[Broadcast]
	public void AddMoney( int money )
	{
		if ( IsProxy )
			return;

		Money += money;
	}

	//[Broadcast]
	//public void AddScoreAndMoney(int score, int money)
	//{
	//	if ( IsProxy )
	//		return;

	//	Score += score;
	//	Money += money;
	//}

	[Broadcast]
	public void AddMatchVictory()
	{
		if ( IsProxy )
			return;

		NumMatchWins++;
	}

	[Broadcast]
	public void AddMatchLoss()
	{
		if ( IsProxy )
			return;

		NumMatchLosses++;
	}

	public int GetUpgradeLevel(UpgradeType upgradeType)
	{
		if ( Manager.Instance.IsUpgradePassive( upgradeType ) )
		{
			if( PassiveUpgrades.ContainsKey( upgradeType ) )
				return PassiveUpgrades[upgradeType];
		}
		else
		{
			if ( ActiveUpgrades.ContainsKey( upgradeType ) )
				return ActiveUpgrades[upgradeType];
		}

		return 0;
	}

	[Broadcast]
	public void HitSkipButton()
	{
		var sfx = Sound.Play( "bubble", Transform.Position );
		if(sfx != null)
		{
			sfx.Pitch = 0.6f;
		}
	}

	[Broadcast]
	public void BuyItem( bool success )
	{
		var sfx = Sound.Play( "bubble", Transform.Position );
		if ( sfx != null )
		{
			sfx.Pitch = success ? 1.2f : 0.4f;
		}
	}

	[Broadcast]
	public void AdjustUpgradeLevel(UpgradeType upgradeType, int amount)
	{
		if(amount > 0)
		{
			Manager.Instance.SpawnFloaterText( 
				Transform.Position.WithZ( 150f ), 
				Manager.Instance.GetFloaterTextForUpgrade( upgradeType ), 
				lifetime: 1.5f, 
				color: Manager.GetColorForRarity( Manager.Instance.GetRarityForUpgrade( upgradeType ) ), 
				velocity: new Vector2( 0f, 35f ), 
				deceleration: 1.8f, 
				startScale: 0.14f, 
				endScale: 0.16f 
			);
		}

		if ( IsProxy )
			return;

		var upgrades = Manager.Instance.IsUpgradePassive(upgradeType) ? PassiveUpgrades : ActiveUpgrades;

		if ( upgrades.ContainsKey(upgradeType) )
			upgrades[upgradeType] = Math.Min( upgrades[upgradeType] + amount, MAX_UPGRADE_LEVEL );
		else
			upgrades.Add(upgradeType, Math.Min( amount, MAX_UPGRADE_LEVEL ) );

		if ( upgrades[upgradeType] <= 0 )
		{
			upgrades.Remove( upgradeType );

			if ( SelectedUpgradeType == upgradeType )
				SelectedUpgradeType = UpgradeType.None;
		}

		if(!Manager.Instance.IsUpgradePassive( upgradeType ) )
		{
			if ( amount > 0 && SelectedUpgradeType == UpgradeType.None )
				SelectedUpgradeType = upgradeType;

			if ( amount < 0 && SelectedUpgradeType == UpgradeType.None )
			{
				foreach ( var pair in ActiveUpgrades )
				{
					SelectedUpgradeType = pair.Key;
					break;
				}
			}
		}
	}

	[Broadcast]
	public void SetUpgradeLevel( UpgradeType upgradeType, int amount )
	{
		if( IsProxy ) 
			return;

		var upgrades = Manager.Instance.IsUpgradePassive( upgradeType ) ? PassiveUpgrades : ActiveUpgrades;

		if ( upgrades.ContainsKey( upgradeType ) )
			upgrades[upgradeType] = Math.Min( upgrades[upgradeType] + amount, MAX_UPGRADE_LEVEL );
		else
			upgrades.Add( upgradeType, Math.Min( amount, MAX_UPGRADE_LEVEL ) );
	}

	[Broadcast]
	public void ClearStats()
	{
		if ( IsProxy )
			return;

		Money = 0;
		HP = MaxHP;
		PassiveUpgrades.Clear();
		ActiveUpgrades.Clear();
		IsInvulnerable = false;
		NumShopItems = 5;

		Money = 448;
	}

	public int GetUpgradeHash()
	{
		int hash = 0;

		foreach ( var upgrade in PassiveUpgrades )
			hash += upgrade.Value;

		foreach ( var upgrade in ActiveUpgrades )
			hash += upgrade.Value;

		return hash;
	}

	[Broadcast]
	public void SetPlayerNum( int playerNum )
	{
		if ( IsProxy )
			return;

		PlayerNum = playerNum;
	}

	[Broadcast]
	public void SetSpectator( bool isSpectator )
	{
		if ( IsProxy )
			return;

		Respawn();
		IsSpectator = isSpectator;
	}

	[Broadcast]
	public void Jump( Vector3 targetPos )
	{
		// animation/sound
		Animator.TriggerJump();

		if ( IsProxy )
			return;

		IsJumping = true;
		_jumpStartPos = Transform.Position;
		_jumpTargetPos = targetPos;
		_jumpTime = 0.33f;
		_timeSinceJump = 0f;
	}

	[Broadcast]
	public void StartInvulnerability()
	{
		_isFlashing = true;
		_timeSinceFlashToggle = 0f;

		if ( IsProxy )
			return;

		IsInvulnerable = true;
		_timeSinceInvulnerableStart = 0f;

		//InnerHitbox.Components.Get<Collider>().Enabled = false;
	}

	[Broadcast]
	public void EndInvulnerability()
	{
		_isFlashing = false;
		SetRendererVisibility( visible: true );

		if ( IsProxy )
			return;

		IsInvulnerable = false;
		//InnerHitbox.Components.Get<Collider>(includeDisabled: true).Enabled = true;
	}

	void SetRendererVisibility(bool visible)
	{
		foreach ( var renderer in Model.Components.GetAll<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants ) )
			renderer.Tint = Color.White.WithAlpha( visible ? 1f : 0.1f );
	}
}
