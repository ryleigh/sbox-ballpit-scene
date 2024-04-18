﻿using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

public class PlayerController : Component, Component.ITriggerListener
{
	[Property] public CitizenAnimationHelper Animator { get; set; }
	[Property] public GameObject Model { get; set; }
	//[Property] public GameObject InnerHitbox { get; set; }
	public RagdollController Ragdoll { get; private set; }

	[Sync] public int PlayerNum { get; set; } = 0;
	public int OpponentPlayerNum => PlayerNum == 0 ? 1 : 0;

	[Sync] public Vector2 Velocity { get; set; }
	[Sync] public float FacingAngle { get; set; }

	public Vector2 Pos2D => (Vector2)Transform.Position;
	//public Vector2 ForwardVec2D => PlayerNum == 0 ? new Vector2( 1f, 0f ) : new Vector2( -1f, 0f );

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
	[Sync] public NetDictionary<UpgradeType, float> PassiveUpgradeProgress { get; set; } = new();
	[Sync] public NetDictionary<UpgradeType, float> UpgradeUseTimes { get; set; } = new();

	[Sync] public UpgradeType SelectedUpgradeType { get; set; }

	private bool _isFlashing;
	private TimeSince _timeSinceFlashToggle;
	private bool _renderersVisible;

	[Sync] public bool IsInvulnerable { get; set; }
	private TimeSince _timeSinceInvulnerableStart;
	public float InvulnerableTime { get; set; } = 1f;

	public float RadiusLarge { get; set; } = 6f;
	public float RadiusSmall { get; set; } = 1.2f;

	public const float BUMP_SPEED_INCREASE_FACTOR_MAX = 1.2f;

	[Sync] public int CurrRerollPrice { get; set; }

	public Dictionary<UpgradeType, Upgrade> LocalUpgrades { get; set; } = new(); // not networked

	public const float SCALE_SPECTATOR = 1.05f;

	[Sync] public float MoneyChangedTime { get; set; }
	[Sync] public float HpChangedTime { get; set; }

	public bool IsIntangible { get; set; }
	private bool _isFading;
	private bool _wasAlreadyFading;
	private TimeSince _timeSinceFade;

	public bool IsBarrierActive { get; set; }

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


		Model.Transform.LocalScale = Vector3.One * SCALE_SPECTATOR;
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
		//Gizmo.Draw.Text( $"{IsIntangible}", new global::Transform( Transform.Position ) );

		Animator.WithVelocity( Velocity );

		if(!IsJumping)
		{
			Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( FacingAngle ), 5f * Time.Delta );

			//if ( IsSpectator )
			//	Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( Utils.VectorToDegrees( Velocity ) ), Velocity.Length * 0.2f * Time.Delta );
			//else
			//	Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( Utils.VectorToDegrees( Manager.Instance.MouseWorldPos - (Vector2)Transform.Position ) ), 5f * Time.Delta );
			//Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( PlayerNum == 0 ? 0f : 180f ), 5f * Time.Delta );
		}

		if( _isFlashing && _timeSinceFlashToggle > 0.04f )
		{
			_renderersVisible = !_renderersVisible;
			SetRenderOpacity( _renderersVisible ? 1f : 0.1f );
			_timeSinceFlashToggle = 0f;
		}

		if ( _isFading )
			HandleFading();

		if ( IsProxy )
			return;

		FacingAngle = Utils.VectorToDegrees( Manager.Instance.MouseWorldPos - (Vector2)Transform.Position );

		if (IsInvulnerable && _timeSinceInvulnerableStart > InvulnerableTime)
		{
			EndInvulnerability();
		}

		if(IsJumping)
		{
			if(_timeSinceJump > _jumpTime)
			{
				IsJumping = false;
				Transform.Position = _jumpTargetPos;
				Model.Transform.LocalScale = Vector3.One * (IsSpectator ? SCALE_SPECTATOR : 1f);
			}
			else
			{
				var progress = Utils.Map( _timeSinceJump, 0f, _jumpTime, 0f, 1f, EasingType.QuadInOut );
				Transform.Position = Vector3.Lerp( _jumpStartPos, _jumpTargetPos, progress );
				Model.Transform.LocalScale = Vector3.One * (IsSpectator ? SCALE_SPECTATOR : 1f) * Utils.MapReturn(progress, 0f, 1f, 1f, 1.6f, EasingType.Linear);
			}

			return;
		}

		if(!IsSpectator) 
		{
			if ( Input.Pressed( "PrevItem" ) )				AdjustSelectedActiveUpgrade( up: false );
			else if ( Input.Pressed( "NextItem" ) )			AdjustSelectedActiveUpgrade( up: true );
			else if ( Input.Pressed( "Slot1" ) )			SetSelectedActiveUpgrade( 0 );
			else if ( Input.Pressed( "Slot2" ) )			SetSelectedActiveUpgrade( 1 );
			else if ( Input.Pressed( "Slot3" ) )			SetSelectedActiveUpgrade( 2 );
			else if ( Input.Pressed( "Slot4" ) )			SetSelectedActiveUpgrade( 3 );
			else if ( Input.Pressed( "Slot5" ) )			SetSelectedActiveUpgrade( 4 );
			else if ( Input.Pressed( "Slot6" ) )			SetSelectedActiveUpgrade( 5 );
			else if ( Input.Pressed( "Slot7" ) )			SetSelectedActiveUpgrade( 6 );
			else if ( Input.Pressed( "Slot8" ) )			SetSelectedActiveUpgrade( 7 );
			else if ( Input.Pressed( "Slot9" ) )			SetSelectedActiveUpgrade( 8 );
			else if ( Input.Pressed( "Slot0" ) )			SetSelectedActiveUpgrade( 9 );
			else if ( Input.MouseWheel != Vector2.Zero )	AdjustSelectedActiveUpgrade( up: Input.MouseWheel.y < 0f );
		}

		if ( IsDead )
			return;

		var wishMoveDir = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x ).Normal;

		float moveSpeed = 90f * MoveSpeedUpgrade.GetIncrease( GetUpgradeLevel( UpgradeType.MoveSpeed ) );
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
				TryUseItem(SelectedUpgradeType);

			CheckBoundsPlaying();

			if ( !IsIntangible && Manager.Instance.GamePhase == GamePhase.RoundActive )
			{
				// collide with balls
				foreach ( var ball in Scene.GetAllComponents<Ball>() )
				{
					if ( ball.IsDespawning )
						continue;

					if ( ball.PlayerNum == PlayerNum )
					{
						if ( ball.TimeSinceBumped > 0.5f &&
							(ball.Transform.Position - Transform.Position).WithZ( 0f ).LengthSquared < MathF.Pow( RadiusLarge + ball.Radius, 2f ) )
						{
							var dir = ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).Normal;

							//Log.Info( $"ball.Velocity: {ball.Velocity.Length} player.Velocity: {Velocity.Length}" );
							var currVel = ball.Velocity.Length;
							var speed = Math.Max( currVel, Math.Min(GetTotalVelocity().Length, currVel * BUMP_SPEED_INCREASE_FACTOR_MAX ) );

							int bumpStrengthLvl = GetUpgradeLevel( UpgradeType.BumpStrength );
							if ( bumpStrengthLvl > 0 )
								speed += BumpStrengthUpgrade.GetIncrease( bumpStrengthLvl );

							speed = Math.Min( speed, Ball.MAX_SPEED );
							ball.Velocity = dir * speed;

							ball.BumpedByPlayer();
							BumpOwnBall( (Vector2)ball.Transform.Position );

							ball.TimeSinceBumped = 0f;
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

		foreach(var pair in LocalUpgrades)
		{
			var upgrade = pair.Value;
			upgrade.Update( Time.Delta );
		}
	}

	public void AdjustSelectedActiveUpgrade(bool up)
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

		Sound.Play( "bubble_ui" );
	}

	public void SetSelectedActiveUpgrade(int index)
	{
		if ( index >= ActiveUpgrades.Count )
			return;

		SelectedUpgradeType = ActiveUpgrades.Keys.ElementAt( index );

		Sound.Play( "bubble_ui" );
	}

	public void SetSelectedActiveUpgrade( UpgradeType upgradeType )
	{
		if ( !ActiveUpgrades.ContainsKey(upgradeType) )
			return;

		SelectedUpgradeType = upgradeType;

		Sound.Play( "bubble_ui" );
	}

	public void TryUseItem(UpgradeType upgradeType)
	{
		if ( !ActiveUpgrades.ContainsKey( upgradeType ) )
			return;

		bool useableNow = Manager.Instance.GamePhase == GamePhase.RoundActive || Manager.Instance.CanUseUpgradeInBuyPhase( upgradeType );
		if ( !useableNow )
		{
			Manager.Instance.PlaySfx( "bubble", Transform.Position, volume: 0.7f, pitch: Game.Random.Float(0.35f, 0.45f) );
			return;
		}

		var upgrade = LocalUpgrades[upgradeType];
		upgrade.Use();

		if ( upgradeType != UpgradeType.None )
			AdjustUpgradeLevel( upgradeType, -1 );

		if ( Manager.Instance.HoveredUpgradeType != UpgradeType.None && !ActiveUpgrades.ContainsKey( Manager.Instance.HoveredUpgradeType ) )
			Manager.Instance.HoveredUpgradeType = UpgradeType.None;
	}

	public void CheckBoundsPlaying()
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
		if ( IsIntangible )
			return;

		foreach ( var player in Scene.GetAllComponents<PlayerController>() )
		{
			if ( player == this || player.IsIntangible )
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
		TakeDamage( ball.Velocity * Game.Random.Float(0.015f, 0.032f) );
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy || IsDead || IsSpectator || IsIntangible )
			return;

		if ( other.GameObject.Tags.Has( "item" ) && Manager.Instance.GamePhase == GamePhase.BuyPhase && Manager.Instance.TimeSincePhaseChange > 0.5f )
		{
			var item = other.Components.Get<ShopItem>();
			if ( item.Price <= Money && GetUpgradeLevel( item.UpgradeType ) < Manager.Instance.GetMaxLevelForUpgrade( item.UpgradeType) )
			{
				AdjustMoney(-item.Price);
				AdjustUpgradeLevel( item.UpgradeType, item.NumLevels );

				BuyItem( success: true );

				item.GameObject.Destroy();
			}
			else
			{
				BuyItem( success: false );
			}
		}
		else if ( other.GameObject.Tags.Has( "pickup_item" ) ) 
		{ 
			var pickupItem = other.Components.Get<PickupItem>();

			Manager.Instance.PlaySfx( "bubble", Transform.Position );
			AdjustUpgradeLevel( pickupItem.UpgradeType, pickupItem.NumLevels );

			pickupItem.DestroyRPC();
		}
		else if ( other.GameObject.Tags.Has( "money_pickup" ) )
		{
			var moneyPickup = other.Components.Get<MoneyPickup>();

			if(moneyPickup.CanBePickedUp || (moneyPickup.MoneyMoveMode == MoneyMoveMode.Tossed && moneyPickup.Transform.Position.y < 20f))
			{
				Manager.Instance.PlaySfx( "bubble", Transform.Position );
				AdjustMoney( moneyPickup.NumLevels );

				moneyPickup.DestroyRPC();
			}
		}
		else if ( other.GameObject.Tags.Has( "skip_button" ) && Manager.Instance.GamePhase == GamePhase.BuyPhase && Manager.Instance.TimeSincePhaseChange > 0.5f )
		{
			other.GameObject.Destroy();
			Manager.Instance.SkipButtonHit();

			HitSkipButton();
		}
		else if ( other.GameObject.Tags.Has( "reroll_button" ) && Manager.Instance.GamePhase == GamePhase.BuyPhase && Manager.Instance.TimeSincePhaseChange > 0.5f )
		{
			if ( CurrRerollPrice <= Money )
			{
				AdjustMoney( -CurrRerollPrice );

				HitRerollButton( success: true );

				other.GameObject.Destroy();
				Manager.Instance.RerollButtonHit( PlayerNum );
			}
			else
			{
				HitRerollButton( success: false );
			}
		}
		else if ( other.GameObject.Tags.Has( "explosion" ) && !IsInvulnerable && Manager.Instance.GamePhase == GamePhase.RoundActive && Manager.Instance.TimeSincePhaseChange > 0.5f )
		{
			var explosion = other.Components.Get<Explosion>();
			if( !explosion.DealtDamage && explosion.TimeSinceSpawn > 0.04f && explosion.TimeSinceSpawn < 0.37f )
			{
				var dir = (Transform.Position - other.Transform.Position).Normal;
				var force = (Vector2)dir * Game.Random.Float( 7f, 12f );
				TakeDamage( force );

				explosion.DealtDamage = true;
			}
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
		if ( IsDead || Manager.Instance.GamePhase != GamePhase.RoundActive )
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

		HpChangedTime = RealTime.Now;

		if ( HP <= 0 )
		{
			Die( force + Velocity * Game.Random.Float(0f, 0.025f) );
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
		SetRenderOpacity( 1f );

		if ( IsProxy )
			return;

		if(IsDead)
		{
			Ragdoll.Unragdoll();
			//MoveToSpawnPoint();
			IsDead = false;
		}
		
		HP = MaxHP;
		HpChangedTime = RealTime.Now;
		IsInvulnerable = false;
		IsIntangible = false;
	}

	[Broadcast]
	public void AdjustMoney( int amount )
	{
		if ( amount == 0 )
			return;

		Manager.Instance.SpawnFloaterText(
			Transform.Position.WithZ( 150f ),
			$"{(amount > 0 ? "+" : "-")}${Math.Abs(amount)}",
			lifetime: amount > 0 ? 1f : 0.9f,
			color: new Color(1f, 1f, 0f),
			velocity: new Vector2( 0f, amount > 0 ? 30f : -35f ),
			deceleration: amount > 0 ? 1.8f : 1.9f,
			startScale: amount > 0 ? 0.15f : 0.16f,
			endScale: amount > 0 ? 0.19f : 0.12f,
			isEmoji: false
		);

		if ( IsProxy )
			return;

		Money += amount;

		MoneyChangedTime = RealTime.Now;
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

	[Broadcast]
	public void IncreaseRerollPrice()
	{
		if ( IsProxy )
			return;

		CurrRerollPrice++;
	}

	[Broadcast]
	public void ResetRerollPrice()
	{
		if ( IsProxy )
			return;

		CurrRerollPrice = 1;
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
	public void HitRerollButton(bool success)
	{
		var sfx = Sound.Play( "bubble", Transform.Position );
		if ( sfx != null )
		{
			sfx.Pitch = success ? 1f : 0.5f;
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
		if ( amount == 0 )
			return;

		var isPassive = Manager.Instance.IsUpgradePassive( upgradeType );
		var upgrades = isPassive ? PassiveUpgrades : ActiveUpgrades;

		var maxLevel = Manager.Instance.GetMaxLevelForUpgrade( upgradeType );
		var currLevel = upgrades.ContainsKey( upgradeType ) ? upgrades[upgradeType] : 0;

		var icon = (currLevel >= maxLevel && amount > 0) ? "🚫" : Manager.Instance.GetIconForUpgrade( upgradeType );

		Manager.Instance.SpawnFloaterText( 
			Transform.Position.WithZ( 150f ),
			//$"{(amount > 0 ? "+" : "-")}{Manager.Instance.GetFloaterTextForUpgrade( upgradeType )}", 
			$"{icon}",
			lifetime: amount > 0 ? 1f : 0.75f, 
			color: Manager.GetColorForRarity( Manager.Instance.GetRarityForUpgrade( upgradeType ) ), 
			velocity: new Vector2( 0f, amount > 0 ? 35f : -65f ), 
			deceleration: amount > 0 ? 1.8f : 2.1f, 
			startScale: amount > 0 ? 0.25f : 0.27f, 
			endScale: amount > 0 ? 0.3f : 0.2f,
			isEmoji: true
		);

		if ( IsProxy )
			return;

		if ( upgrades.ContainsKey(upgradeType) )
			upgrades[upgradeType] = Math.Min( upgrades[upgradeType] + amount, maxLevel );
		else
			upgrades.Add(upgradeType, Math.Min( amount, maxLevel ) );

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

		if( LocalUpgrades.ContainsKey( upgradeType ) )
		{
			var upgrade = LocalUpgrades[upgradeType];

			var newLevel = Math.Min( upgrade.Level + amount, maxLevel );
			if ( newLevel != upgrade.Level )
				upgrade.SetLevel( newLevel );
		}
		else
		{
			var className = $"{upgradeType}Upgrade";
			var upgrade = TypeLibrary.Create<Upgrade>( className );
			upgrade.Init( this, Manager.Instance, Scene, isPassive );
			upgrade.SetLevel( amount );
			LocalUpgrades.Add( upgradeType, upgrade );
		}

		UpgradeUseTimes[upgradeType] = RealTime.Now;
	}

	[Broadcast]
	public void ClearStats()
	{
		if ( IsProxy )
			return;

		Money = 0;
		HP = MaxHP;
		HpChangedTime = RealTime.Now;
		PassiveUpgrades.Clear();
		ActiveUpgrades.Clear();
		PassiveUpgradeProgress.Clear();
		UpgradeUseTimes.Clear();
		IsInvulnerable = false;
		IsIntangible = false;
		NumShopItems = 4;
		CurrRerollPrice = 1;

		Money = 5;
	}

	[Broadcast]
	public void ClearUpgradeProgress()
	{
		if ( IsProxy ) 
			return;

		PassiveUpgradeProgress.Clear();

		foreach ( var pair in LocalUpgrades )
		{
			var upgrade = pair.Value;
			upgrade.ClearProgress();
		}
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
		SetRenderOpacity( 1f );

		if ( IsProxy )
			return;

		IsInvulnerable = false;
		//InnerHitbox.Components.Get<Collider>(includeDisabled: true).Enabled = true;
	}

	[Broadcast]
	public void SetRenderOpacityRPC( float opacity )
	{
		SetRenderOpacity( opacity );
	}

	public void SetRenderOpacity(float opacity)
	{
		foreach ( var renderer in Model.Components.GetAll<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants ) )
			renderer.Tint = Color.White.WithAlpha( opacity );
	}

	public Vector2 GetTotalVelocity()
	{
		var vel = Velocity;

		if( LocalUpgrades.ContainsKey(UpgradeType.Dash) )
		{
			vel += ((DashUpgrade)LocalUpgrades[UpgradeType.Dash]).DashVelocity;
		}

		return vel;
	}

	[Broadcast]
	public void FadeStart()
	{
		IsIntangible = true;
		_isFlashing = false;

		_wasAlreadyFading = _isFading;

		_isFading = true;
		_timeSinceFade = 0f;
	}

	void HandleFading()
	{
		if(_timeSinceFade < 0.2f && !_wasAlreadyFading)
		{
			SetRenderOpacityRPC( Utils.Map( _timeSinceFade, 0, 0.2f, 1f, 0.2f ) );
		}
		else if(_timeSinceFade < 0.8f)
		{
			// do nothing
		}
		else if( _timeSinceFade < 1f)
		{
			SetRenderOpacityRPC( Utils.Map( _timeSinceFade, 0.8f, 1f, 0.2f, 1f ) );
		}
		else
		{
			_isFading = false;
			IsIntangible = false;
			SetRenderOpacityRPC( 1f );
		}
	}

	public void SetBarrierVisible( bool visible )
	{
		var barrier = PlayerNum == 0 ? Manager.Instance.BarrierLeft : Manager.Instance.BarrierRight;

		barrier.Enabled = visible;
		//barrier.Components.Get<ModelRenderer>( FindMode.EverythingInSelf ).Enabled = visible;
	}

	//private CancellationTokenSource _fadeCts;

	//[Broadcast]
	//public void FadeStart()
	//{
	//	IsIntangible = true;
	//	_isFlashing = false;

	//	if( _fadeCts != null )
	//		_fadeCts.Cancel();

	//	_fadeCts = new CancellationTokenSource();
	//	_ = Fade( _fadeCts.Token );
	//}

	//async Task Fade(CancellationToken cts)
	//{
	//	try
	//	{
	//		for ( int i = 0; i < 5; i++ )
	//		{
	//			SetRenderOpacityRPC( Utils.Map( i, 0, 5, 0.8f, 0.2f ) );
	//			await Task.Delay( 20 );
	//		}

	//		await Task.Delay( 800 );

	//		for ( int i = 0; i < 5; i++ )
	//		{
	//			SetRenderOpacityRPC( Utils.Map( i, 0, 5, 0.2f, 0.8f ) );
	//			await Task.Delay( 20 );
	//		}

	//		SetRenderOpacityRPC( 1f );
	//		IsIntangible = false;
	//	}
	//	catch( OperationCanceledException )
	//	{
	//		Log.Info( $"canceled!" );
	//	}
	//}
}
