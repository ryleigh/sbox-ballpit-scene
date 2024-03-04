using Sandbox;
using Sandbox.Citizen;
using Sandbox.UI;
using System.Numerics;
using System.Reflection.Metadata;

public class PlayerController : Component, Component.ITriggerListener
{
	[Property] public CitizenAnimationHelper Animator { get; set; }
	[Property] public GameObject Model { get; set; }
	public RagdollController Ragdoll { get; private set; }

	[Sync] public int PlayerNum { get; set; } = 0;
	public int OpponentPlayerNum => PlayerNum == 0 ? 1 : 0;

	[Sync] public Vector2 Velocity { get; set; }

	public Vector2 Pos2D => (Vector2)Transform.Position;
	public Vector2 ForwardVec2D => PlayerNum == 0 ? new Vector2( 1f, 0f ) : new Vector2( -1f, 0f );

	[Sync] public bool IsDead { get; set; }

	[Sync] public int HP { get; set; }
	public int MaxHP { get; set; } = 3;

	[Sync] public bool IsSpectator { get; set; }
	[Sync] public int Score { get; private set; }
	[Sync] public int Money { get; private set; }

	[Sync] public NetDictionary<UpgradeType, int> Upgrades { get; set; } = new();
	public const int MAX_UPGRADE_LEVEL = 10;

	[Sync] public UpgradeType SelectedUpgradeType { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Ragdoll = Components.GetInDescendantsOrSelf<RagdollController>();
		HP = MaxHP;
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
			return;

	}

	protected override void OnUpdate()
	{
		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.95f );
		//Gizmo.Draw.Text( $"{SelectedUpgradeType}", new global::Transform( Transform.Position ) );

		Animator.WithVelocity( Velocity * (Velocity.y > 0f ? 0.7f : 0.6f));

		if(IsSpectator)
			Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( Utils.VectorToDegrees(Velocity) ), Velocity.Length * 0.2f * Time.Delta );
		else
			Model.Transform.LocalRotation = Rotation.Lerp( Model.Transform.LocalRotation, Rotation.FromYaw( PlayerNum == 0 ? 0f : 180f ), 5f * Time.Delta );

		if ( IsProxy )
			return;

		if(!IsDead)
		{
			var wishMoveDir = new Vector2( -Input.AnalogMove.y, Input.AnalogMove.x ).Normal;

			float moveSpeed = Utils.Map( GetUpgradeLevel( UpgradeType.MoveSpeed ), 0, 10, 90f, 125f, EasingType.SineOut );
			Velocity = Utils.DynamicEaseTo( Velocity, wishMoveDir * moveSpeed, 0.2f, Time.Delta );
			Transform.Position += (Vector3)Velocity * Time.Delta;
			Transform.Position = Transform.Position.WithZ( IsSpectator ? 80f : 0f );
		}
		
		if ( IsSpectator )
		{
			CheckBoundsSpectator();
		}
		else
		{
			if(Manager.Instance.GamePhase == GamePhase.RoundActive && Input.Pressed("Jump"))
			{
				TryUseItem();
			}

			CheckBoundsPlaying();
		}
	}

	void TryUseItem()
	{
		switch(SelectedUpgradeType)
		{
			case UpgradeType.None: default:
				break;
			case UpgradeType.ShootBalls:
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
		}

		if ( SelectedUpgradeType != UpgradeType.None )
			AdjustUpgradeLevel( SelectedUpgradeType, -1 );
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

	public void HitOwnBall( Ball ball )
	{
		ball.HitByPlayer( direction: ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).Normal );
	}

	public void HitOpponentBall( Ball ball )
	{
		TakeDamage( ball.Velocity * 0.025f );
		ball.HitPlayer( GameObject.Id );
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy || IsDead || IsSpectator )
			return;

		if(other.GameObject.Tags.Has("ball") && Manager.Instance.GamePhase == GamePhase.RoundActive )
		{
			var ball = other.Components.Get<Ball>();
			if (ball.IsActive && ball.PlayerNum == PlayerNum)
			{
				HitOwnBall( ball );
			}
		}
		else if(other.GameObject.Tags.Has("item") && Manager.Instance.GamePhase == GamePhase.BuyPhase && Manager.Instance.TimeSincePhaseChange > 2f)
		{
			var item = other.Components.Get<ShopItem>();
			if(item.Price <= Money)
			{
				Money -= item.Price;
				AdjustUpgradeLevel( item.UpgradeType, item.NumLevels );

				item.GameObject.Destroy();
			}
		}
		else if ( other.GameObject.Tags.Has( "skip_button" ) && Manager.Instance.GamePhase == GamePhase.BuyPhase && Manager.Instance.TimeSincePhaseChange > 2f )
		{
			other.GameObject.Destroy();
			Manager.Instance.SkipButtonHit();
		}
	}

	public void OnTriggerExit( Collider other )
	{
		if ( IsProxy )
			return;

	}

	[Broadcast]
	public void TakeDamage( Vector2 force )
	{
		if ( IsDead )
			return;

		// todo: flash, sfx, etc

		if(IsProxy)
			return;

		HP--;

		if ( HP <= 0 )
		{
			Die( force );
		}
		else
		{
			Manager.Instance.PlayerHit(GameObject.Id);
		}
	}

	[Broadcast]
	public void Die(Vector3 force)
	{
		if ( IsProxy )
			return;

		Ragdoll.Ragdoll( Transform.Position + Vector3.Up * 100f, force );

		IsDead = true;
		Velocity = Vector2.Zero;

		Manager.Instance.PlayerDied( GameObject.Id );
	}

	[Broadcast]
	public void Respawn()
	{
		if ( IsProxy )
			return;

		if(IsDead)
		{
			Ragdoll.Unragdoll();
			//MoveToSpawnPoint();
			IsDead = false;
		}
		
		HP = MaxHP;
	}

	[Broadcast]
	public void AddScoreAndMoney(int score, int money)
	{
		if ( IsProxy )
			return;

		Score += score;
		Money += money;
	}

	public int GetUpgradeLevel(UpgradeType upgradeType)
	{
		if(Upgrades.ContainsKey(upgradeType))
			return Upgrades[upgradeType];

		return 0;
	}

	[Broadcast]
	public void AdjustUpgradeLevel(UpgradeType upgradeType, int amount)
	{
		if ( IsProxy )
			return;

		if ( Upgrades.ContainsKey(upgradeType) )
			Upgrades[upgradeType] = Math.Min( Upgrades[upgradeType] + amount, MAX_UPGRADE_LEVEL );
		else
			Upgrades.Add(upgradeType, Math.Min( amount, MAX_UPGRADE_LEVEL ) );

		if ( Upgrades[upgradeType] <= 0 )
		{
			Upgrades.Remove( upgradeType );

			if ( SelectedUpgradeType == upgradeType )
				SelectedUpgradeType = UpgradeType.None;
		}

		if ( amount > 0 && !Globals.IsUpgradePassive( upgradeType ) && SelectedUpgradeType == UpgradeType.None )
			SelectedUpgradeType = upgradeType;
	}

	[Broadcast]
	public void SetUpgradeLevel( UpgradeType upgradeType, int amount )
	{
		if( IsProxy ) 
			return;

		if ( Upgrades.ContainsKey( upgradeType ) )
			Upgrades[upgradeType] = Math.Min( Upgrades[upgradeType] + amount, MAX_UPGRADE_LEVEL );
		else
			Upgrades.Add( upgradeType, Math.Min( amount, MAX_UPGRADE_LEVEL ) );
	}

	[Broadcast]
	public void StartNewMatch()
	{
		if ( IsProxy )
			return;

		Score = 0;
		Money = 0;
		HP = MaxHP;
	}

	public int GetUpgradeHash()
	{
		int hash = 0;

		foreach ( var upgrade in Upgrades )
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
}
