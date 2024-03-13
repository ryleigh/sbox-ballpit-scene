using Sandbox;
using Sandbox.Network;
using Sandbox.UI;
using System.Diagnostics.Metrics;
using System.IO;
using System.Numerics;
using System.Threading.Channels;

public enum GamePhase { WaitingForPlayers, StartingNewMatch, RoundActive, RoundFinished, BuyPhase }

public sealed class Manager : Component, Component.INetworkListener
{
	public static Manager Instance { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject BallPrefab { get; set; }
	[Property] public GameObject SkipButtonPrefab { get; set; }
	[Property] public GameObject ShopItemPrefab { get; set; }
	[Property] public GameObject BallExplosionParticles { get; set; }
	[Property] public GameObject BallGutterParticles { get; set; }

	[Property, Sync] public Color ColorPlayer0 { get; set; }
	[Property, Sync] public Color ColorPlayer1 { get; set; }

	public const float X_FAR = 228f;
	public const float X_CLOSE = 21f;

	public const float Y_LIMIT = 110.3f;

	public const float BALL_HEIGHT_SELF = 45f;
	public const float BALL_HEIGHT_OPPONENT = 55f;

	public const float SPECTATOR_HEIGHT = 80f;

	public PlayerController Player0 { get; set; }
	public PlayerController Player1 { get; set; }
	[Sync] public Guid PlayerId0 { get; set; }
	[Sync] public Guid PlayerId1 { get; set; }
	[Sync] public bool DoesPlayerExist0 { get; set; }
	[Sync] public bool DoesPlayerExist1 { get; set; }

	public int NumActivePlayers => (DoesPlayerExist0 ? 1 : 0) + (DoesPlayerExist1 ? 1 : 0);

	[Sync] public int RoundNum { get; private set; }

	[Sync] public GamePhase GamePhase { get; private set; }
	[Sync] public TimeSince TimeSincePhaseChange { get; private set; }
	private int _numBuyPhaseSkips;

	public const float START_NEW_MATCH_DELAY = 5f;

	public Vector3 OriginalCameraPos { get; private set; }
	public Rotation OriginalCameraRot { get; private set; }

	public Dispenser Dispenser { get; private set; }

	public bool IsSlowmo { get; set; }
	private float _slowmoTime;
	private float _slowmoTimeScale;
	private RealTimeSince _realTimeSinceSlowmoStarted;
	private EasingType _slowmoEasingType;
	public const float ROUND_FINISHED_DELAY = 4f;
	public float BuyPhaseDuration { get; private set; } = 30f;

	public GameObject HoveredObject { get; private set; }
	public UpgradeType HoveredUpgradeType { get; set; }
	public Vector2 HoveredUpgradePos { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;

		var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		OriginalCameraPos = camera.Transform.Position;
		OriginalCameraRot = camera.Transform.Rotation;

		Dispenser = Scene.GetAllComponents<Dispenser>().FirstOrDefault();
	}

	protected override void OnStart()
	{
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
		}

		if ( Networking.IsHost )
			Network.TakeOwnership();

		if ( IsProxy )
			return;

		GamePhase = GamePhase.WaitingForPlayers;

		//CreateShopItem( 0, new Vector2( -215f, -20f ), UpgradeType.MoveSpeed, 1, 3 );

		StartNewMatch();
		StartNewRound();
	}

	public void OnActive( Connection channel )
	{
		//Log.Info( $"Player '{channel.DisplayName}' is becoming active (local = {channel == Connection.Local}) (host = {channel.IsHost})" );

		var playerObj = PlayerPrefab.Clone( Vector3.Zero );
		var player = playerObj.Components.Get<PlayerController>();

		//var copter = copterObj.Components.Get<Copter>();
		//copter.SetBaseColor( new Color( 0.07f, 0.16f, 0.83f ) );

		//copterObj.Components.Create<CopterPlayer>();
		var clothing = new ClothingContainer();
		clothing.Deserialize( channel.GetUserData( "avatar" ) );
		clothing.Apply( playerObj.Components.GetInChildren<SkinnedModelRenderer>() );

		if ( !DoesPlayerExist0 )
		{
			Player0 = player;
			PlayerId0 = player.GameObject.Id;
			DoesPlayerExist0 = true;
			player.PlayerNum = 0;
		}
		else if ( !DoesPlayerExist1 )
		{
			Player1 = player;
			PlayerId1 = player.GameObject.Id;
			DoesPlayerExist1 = true;
			player.PlayerNum = 1;
		}
		else
		{
			player.IsSpectator = true;
		}

		playerObj.NetworkSpawn( channel );

		//player.AdjustUpgradeLevel( UpgradeType.MoveSpeed, 1 );
		//player.AdjustUpgradeLevel( UpgradeType.ShootBalls, 3 );

		//if ( channel.IsHost )
		//{
		//	CopterGameManager.Instance.HostConnected();
		//}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		DebugDisplay();

		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"{HoveredObject}", new global::Transform( Vector3.Zero ) );

		if (IsSlowmo)
		{
			if(_realTimeSinceSlowmoStarted > _slowmoTime)
			{
				IsSlowmo = false;
				Scene.TimeScale = 1f;
			}
			else
			{
				Scene.TimeScale = Utils.Map( _realTimeSinceSlowmoStarted, 0f, _slowmoTime, _slowmoTimeScale, 1f, _slowmoEasingType );
			}
		}

		HoveredObject = null;
		var camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		var tr = Scene.Trace.Ray( camera.ScreenPixelToRay( Mouse.Position ), 1000f ).HitTriggers().Run();
		if ( tr.Hit )
		{
			HoveredObject = tr.GameObject;
		}

		if ( IsProxy )
			return;

		switch (GamePhase)
		{
			case GamePhase.WaitingForPlayers:
				if(DoesPlayerExist0 && DoesPlayerExist1)
				{
					StartNewMatch();
				}
				break;
			case GamePhase.StartingNewMatch:
				if ( TimeSincePhaseChange > START_NEW_MATCH_DELAY )
					StartNewRound();
				break;
			case GamePhase.RoundActive:
				break;
			case GamePhase.RoundFinished:
				if ( TimeSincePhaseChange > ROUND_FINISHED_DELAY )
					StartBuyPhase();
				break;
			case GamePhase.BuyPhase:
				if ( TimeSincePhaseChange > BuyPhaseDuration )
					FinishBuyPhase();
				break;
		}
	}

	void DebugDisplay()
	{
		string str = "";
		foreach ( var player in Scene.GetAllComponents<PlayerController>() )
		{
			str += $"{player.Network.OwnerConnection.DisplayName}";
			str += $"{(player.Network.IsOwner ? " (local)" : "")}";
			str += $"{(player.Network.OwnerConnection.IsHost ? " (host)" : "")}";
			str += $"{(player.IsDead ? " 💀" : "")}";

			if ( DoesPlayerExist0 && player.GameObject.Id == PlayerId0 )
				str += $" ..... PLAYER 0";

			if ( DoesPlayerExist1 && player.GameObject.Id == PlayerId1 )
				str += $" ..... PLAYER 1";

			str += $"\n";
		}
		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.ScreenText( str, new Vector2( 5f, 5f ), size: 12, flags: TextFlag.Left );
	}

	[Broadcast]
	public void PlayerDied(Guid id)
	{
		Slowmo( 0.125f, 2f, EasingType.SineOut );

		TimeSincePhaseChange = 0f;

		if ( IsProxy )
			return;

		var playerObj = Scene.Directory.FindByGuid( id );
		if ( playerObj != null )
		{
			var player = playerObj.Components.Get<PlayerController>();
			player.AddScoreAndMoney( score: 0, money: 5 );

			var otherPlayer = GetPlayer( GetOtherPlayerNum( player.PlayerNum ) );
			if ( otherPlayer != null )
				otherPlayer.AddScoreAndMoney( score: 1, money: 10 );
		}

		FinishRound();
	}

	void StartNewMatch()
	{
		RoundNum = 0;
		Player0?.StartNewMatch();
		Player1?.StartNewMatch();

		GamePhase = GamePhase.StartingNewMatch;
		TimeSincePhaseChange = 0f;
	}

	void StartNewRound()
	{
		RoundNum++;
		GamePhase = GamePhase.RoundActive;
		TimeSincePhaseChange = 0f;

		Dispenser.StartWave();
	}

	void FinishRound()
	{
		GamePhase = GamePhase.RoundFinished;

		DestroyBalls();
	}

	void StartBuyPhase()
	{
		GamePhase = GamePhase.BuyPhase;
		TimeSincePhaseChange = 0f;

		Player0?.Respawn();
		Player1?.Respawn();

		if ( IsProxy )
			return;

		_numBuyPhaseSkips = 0;

		if ( DoesPlayerExist0 )
		{
			CreateSkipButton( 0 );
			CreateShopItem( 0, new Vector2( -215f, -20f ), UpgradeType.MoveSpeed, 1, 3 );
			CreateShopItem( 0, new Vector2( -215f, 20f ), UpgradeType.ShootBalls, 2, 4 );
		}

		if ( DoesPlayerExist1 )
		{
			CreateSkipButton( 1 );
			CreateShopItem( 1, new Vector2( 215f, -20f ), UpgradeType.MoveSpeed, 1, 3 );
			CreateShopItem( 1, new Vector2( 215f, 20f ), UpgradeType.ShootBalls, 2, 4 );
		}
	}

	void FinishBuyPhase()
	{
		DestroyShopStuff();

		StartNewRound();
	}

	void CreateSkipButton( int playerNum )
	{
		var skipButtonObj = SkipButtonPrefab.Clone( new Vector3( 30f * (playerNum == 0 ? -1f : 1f), 103f, 0f ) );
		skipButtonObj.NetworkSpawn( GetConnection( playerNum ) );
	}

	void CreateShopItem( int playerNum, Vector2 pos, UpgradeType upgradeType, int numLevels, int price )
	{
		var shopItemObj = ShopItemPrefab.Clone( new Vector3( pos.x, pos.y, 0f ) );
		shopItemObj.NetworkSpawn( GetConnection( playerNum ) );
		shopItemObj.Components.Get<ShopItem>().Init( upgradeType, numLevels, price );
	}

	[Broadcast]
	public void SkipButtonHit()
	{
		if ( IsProxy || GamePhase != GamePhase.BuyPhase )
			return;

		_numBuyPhaseSkips++;

		if( _numBuyPhaseSkips >= NumActivePlayers )
		{
			FinishBuyPhase();
		}
	}

	public void SpawnBall(Vector2 pos, Vector2 velocity, int playerNum)
	{
		var height = (playerNum == 0 && pos.x > 0f || playerNum == 1 && pos.x < 0f) ? BALL_HEIGHT_OPPONENT : BALL_HEIGHT_SELF;
		var ballObj = BallPrefab.Clone( new Vector3(pos.x, pos.y, height ) );
		var ball = ballObj.Components.Get<Ball>();

		ball.Velocity = velocity;

		//Log.Info( $"SpawnBall - connection: {connection}" );

		//ballObj.NetworkSpawn( GetConnection( playerNum ) );
		ballObj.NetworkSpawn();

		ball.SetPlayerNum( playerNum );

		//int side = pos.x > 0f ? 1 : 0;
		//ballObj.NetworkSpawn(GetConnection(side));
	}

	[Broadcast]
	public void SetPlayerActive(int playerNum, Guid id)
	{
		if (IsProxy)
			return;

		if ( (DoesPlayerExist0 && PlayerId0 == id) || (DoesPlayerExist0 && PlayerId0 == id) )
			return;

		if ( (playerNum == 0 && DoesPlayerExist0) || (playerNum == 1 && DoesPlayerExist1) )
			return;

		var playerObj = Scene.Directory.FindByGuid( id );
		if ( playerObj == null )
			return;

		var player = playerObj.Components.Get<PlayerController>();
		player.SetPlayerNum( playerNum );
		player.SetSpectator( false );

		if ( playerNum == 0 )
		{
			Player0 = player;
			PlayerId0 = id;
			DoesPlayerExist0 = true;
		}
		else if ( playerNum == 1 )
		{
			Player1 = player;
			PlayerId1 = id;
			DoesPlayerExist1 = true;
		}
	}

	[Broadcast]
	public void PlayerHit( Guid id )
	{
		Slowmo(0.025f, 1f, EasingType.SineOut);

		if ( IsProxy )
			return;
	}

	public Connection GetConnection(int playerNum)
	{
		if( playerNum == 0 && DoesPlayerExist0)
			return Scene.Directory.FindByGuid( PlayerId0 ).Network.OwnerConnection;
		else if(playerNum == 1 && DoesPlayerExist1)
			return Scene.Directory.FindByGuid( PlayerId1 ).Network.OwnerConnection;

		return null;
	}

	public PlayerController GetPlayer(int playerNum)
	{
		if(playerNum == 0 && DoesPlayerExist0)
			return Scene.Directory.FindByGuid( PlayerId0 ).Components.Get<PlayerController>();
		else if(playerNum == 1 && DoesPlayerExist1)
			return Scene.Directory.FindByGuid( PlayerId1 ).Components.Get<PlayerController>();

		return null;
	}

	public static int GetOtherPlayerNum(int playerNum)
	{
		return playerNum == 0 ? 1 : 0;
	}

	public void OnDisconnected( Connection channel )
	{
		if(IsProxy)
			return;

		Log.Info( $"OnDisconnected: {channel.DisplayName} (local = {channel == Connection.Local}) (host = {channel.IsHost})" );

		bool activePlayerLeft = false;

		if ( DoesPlayerExist0 && channel == GetConnection( 0 ) )
		{
			DoesPlayerExist0 = false;
			Player0 = null;
			PlayerId0 = Guid.Empty;
			activePlayerLeft = true;
		}
		else if (DoesPlayerExist1 && channel == GetConnection( 1 ) )
		{
			DoesPlayerExist1 = false;
			Player1 = null;
			PlayerId1 = Guid.Empty;
			activePlayerLeft = true;
		}

		if( activePlayerLeft )
		{
			StopCurrentMatch();
		}

		Log.Info( $"Player1: {Player1}, (1GetConnection): {GetConnection( 1 )}, channel 1? {(GetConnection(1) == channel)}" );

		// check if active players still exist, and stop match if one of them left
		// if spectators exist, fill the spot with one of them who has played the least matches
	}

	void StopCurrentMatch()
	{
		Log.Info( $"StopCurrentMatch" );

		Player0?.Respawn();
		Player1?.Respawn();

		DestroyBalls();
		DestroyShopStuff();

		GamePhase = GamePhase.WaitingForPlayers;
	}

	public void Slowmo(float timeScale, float time, EasingType easingType)
	{
		IsSlowmo = true;
		Scene.TimeScale = timeScale;

		_slowmoTime = time;
		_slowmoTimeScale = timeScale;
		_realTimeSinceSlowmoStarted = 0f;
		_slowmoEasingType = easingType;
	}

	void DestroyBalls()
	{
		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( ball.IsActive )
				ball.Despawn();
		}
	}

	void DestroyShopStuff()
	{
		foreach ( var skipButton in Scene.GetAllComponents<SkipButton>() )
			skipButton.DestroyButton();

		foreach ( var shopItem in Scene.GetAllComponents<ShopItem>() )
			shopItem.DestroyButton();
	}

	public PlayerController GetLocalPlayer()
	{
		foreach ( var player in Scene.GetAllComponents<PlayerController>() )
		{
			if ( player.Network.IsOwner )
				return player;
		}

		return null;
	}

	[Broadcast]
	public void PlayerHitJoinButton( int playerNum, Guid id )
	{
		var playerObj = Scene.Directory.FindByGuid( id );
		if ( playerObj == null )
			return;

		var sfx = Sound.Play( "bubble", playerObj.Transform.Position );
		if ( sfx != null )
			sfx.Pitch = 1.1f;

		SetPlayerActive( playerNum, id );

		var targetPos = new Vector3( 114f * (playerNum == 0 ? -1f : 1f), 0f, 0f);
		playerObj.Components.Get<PlayerController>().Jump( targetPos );
	}

	[Broadcast]
	public void PlayerForfeited(Guid id)
	{
		var playerObj = Scene.Directory.FindByGuid( id );
		if ( playerObj != null )
		{
			var sfx = Sound.Play( "bubble", playerObj.Transform.Position );
			if ( sfx != null )
				sfx.Pitch = 0.7f;
		}

		if (IsProxy)
			return;

		var player = playerObj.Components.Get<PlayerController>();
		player.SetSpectator( true );

		player.Transform.Position = player.Transform.Position.WithZ( SPECTATOR_HEIGHT );
		var targetPos = player.GetClosestSpectatorPos( player.Transform.Position );
		playerObj.Components.Get<PlayerController>().Jump( targetPos );

		if (DoesPlayerExist0 && PlayerId0 == id)
		{
			DoesPlayerExist0 = false;
			Player0 = null;
			PlayerId0 = Guid.Empty;

			if ( GamePhase != GamePhase.WaitingForPlayers )
				StopCurrentMatch();
		}
		else if ( DoesPlayerExist1 && PlayerId1 == id )
		{
			DoesPlayerExist1 = false;
			Player1 = null;
			PlayerId1 = Guid.Empty;

			if ( GamePhase != GamePhase.WaitingForPlayers )
				StopCurrentMatch();
		}
	}

	public void CreateBallExplosionParticles(Vector3 pos, int playerNum )
	{
		var explosionObj = BallExplosionParticles.Clone( pos );
		var particleEffect = explosionObj.Components.Get<ParticleEffect>();
		particleEffect.Tint = playerNum == 0 ? Color.Blue : Color.Green;
	}

	public void CreateBallGutterParticles( Vector3 pos, int playerNum )
	{
		pos += new Vector3( Game.Random.Float(25f, 40f) * (playerNum == 0 ? -1f : 1f), 0f, 0f );
		var particleObj = BallGutterParticles.Clone( pos );
		var particleEffect = particleObj.Components.Get<ParticleEffect>();
		particleEffect.Tint = playerNum == 0 ? Color.Blue : Color.Green;
		particleObj.Transform.Rotation = Rotation.LookAt(new Vector3( playerNum == 0 ? 1f : -1f, 0f, 0f));
	}
}
