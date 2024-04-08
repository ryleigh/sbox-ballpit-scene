using Sandbox;
using Sandbox.Network;
using Sandbox.UI;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Numerics;
using System.Threading.Channels;
using System.Threading.Tasks;

public enum GamePhase { WaitingForPlayers, StartingNewMatch, RoundActive, AfterRoundDelay, BuyPhase, Victory }

public enum UpgradeType { None, MoveSpeed, Volley, Gather, Repel, Replace, Blink, Scatter, Slowmo, Dash, Redirect, BumpStrength, Converge, Autoball, }
public enum UpgradeRarity { Common, Uncommon, Rare, Epic, Legendary }

public struct UpgradeData
{
	public string name;
	public string icon;
	public UpgradeRarity rarity;
	public int maxLevel;
	public bool isPassive;
	public bool useableInBuyPhase;
	public int amountMin;
	public int amountMax;
	public int pricePerAmountMin;
	public int pricePerAmountMax;

	public UpgradeData( string _name, string _icon, UpgradeRarity _rarity, int _maxLevel, int _amountMin, int _amountMax, int _pricePerAmountMin, int _pricePerAmountMax, bool _isPassive, bool _usableInBuyPhase )
	{
		name = _name;
		icon = _icon;
		rarity = _rarity;
		maxLevel = _maxLevel;
		amountMin = _amountMin;
		amountMax = _amountMax;
		pricePerAmountMin = _pricePerAmountMin;
		pricePerAmountMax = _pricePerAmountMax;
		isPassive = _isPassive;
		useableInBuyPhase = _usableInBuyPhase;
	}
}

public sealed class Manager : Component, Component.INetworkListener
{
	public static Manager Instance { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject BallPrefab { get; set; }
	[Property] public GameObject SkipButtonPrefab { get; set; }
	[Property] public GameObject RerollButtonPrefab { get; set; }
	[Property] public GameObject ShopItemPrefab { get; set; }
	[Property] public GameObject ShopItemPassivePrefab { get; set; }
	[Property] public GameObject PickupItemPrefab { get; set; }
	[Property] public GameObject MoneyPickupPrefab { get; set; }
	[Property] public GameObject FadingTextPrefab { get; set; }
	[Property] public GameObject FloaterTextPrefab { get; set; }
	[Property] public GameObject BallExplosionParticles { get; set; }
	[Property] public GameObject BallGutterParticles { get; set; }
	[Property] public GameObject SlidingGround { get; set; }

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
	private int _numSecondsLeftInPhase;
	private int _numBuyPhaseSkips;

	public const float START_NEW_MATCH_DELAY = 5f;
	//public const int MATCH_MAX_SCORE = 10;

	[Sync] public string WinningPlayerName { get; set; }

	public CameraComponent Camera { get; private set; }
	public Vector3 OriginalCameraPos { get; private set; }
	public Rotation OriginalCameraRot { get; private set; }

	public Dispenser Dispenser { get; private set; }

	public bool IsSlowmo { get; set; }
	private float _slowmoTime;
	private float _slowmoTimeScale;
	private RealTimeSince _realTimeSinceSlowmoStarted;
	private EasingType _slowmoEasingType;

	public const float BETWEEN_ROUNDS_DELAY = 4f;
	public const float VICTORY_DELAY = 10f;
	public float BuyPhaseDuration { get; private set; } = 30f;

	public GameObject HoveredObject { get; private set; }
	public UpgradeType HoveredUpgradeType { get; set; }
	public int HoveredUpgradePlayerNum { get; set; }
	public Vector2 HoveredUpgradePos { get; set; }

	[Sync] public float CenterLineOffset { get; set; }
	private float _targetCenterLineOffset;

	[Sync] public int CurrentScore { get; set; }
	public const int SCORE_NEEDED_TO_WIN = 5;

	private int _roundWinnerPlayerNum;
	private bool _hasIncrementedScore;

	public Dictionary<UpgradeType, UpgradeData> UpgradeDatas { get; private set; } = new();
	public Dictionary<UpgradeRarity, List<UpgradeType>> UpgradesByRarity = new();

	public Vector2 MouseWorldPos { get; private set; }

	[Sync] public float TimeScale { get; set; }

	private TimeSince _timeSincePickupSpawn;
	private float _pickupSpawnDelay;

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;

		Camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
		OriginalCameraPos = Camera.Transform.Position;
		OriginalCameraRot = Camera.Transform.Rotation;

		Dispenser = Scene.GetAllComponents<Dispenser>().FirstOrDefault();

		GenerateUpgrades();
	}

	protected override void OnStart()
	{
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
		}

		//Network.SetOrphanedMode( NetworkOrphaned.Random );

		if ( Networking.IsHost )
			Network.TakeOwnership();

		if ( IsProxy )
			return;

		TimeScale = 1f;
		GamePhase = GamePhase.WaitingForPlayers;

		//CreateShopItem( 0, new Vector2( -215f, -20f ), UpgradeType.MoveSpeed, numLevels: 1, price: 3 );
		//CreateShopItem( 0, new Vector2( -215f, 20f ), UpgradeType.Volley, numLevels: 2, price: 4 );
		//CreateShopItem( 0, new Vector2( -215f, -60f ), UpgradeType.Repel, numLevels: 1, price: 0 );

		StartBuyPhase();
		//StartNewMatch();
		//StartNewRound();

		//var upgrade = TypeLibrary.Create<Upgrade>( name );
		//upgrade.Test();
	}

	public void OnActive( Connection channel )
	{
		//Log.Info( $"Player '{channel.DisplayName}' is becoming active (local = {channel == Connection.Local}) (host = {channel.IsHost})" );

		var playerObj = PlayerPrefab.Clone( new Vector3( 0f, 500f, 500f ) );
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

		player.ClearStats();
		playerObj.NetworkSpawn( channel );

		player.AdjustUpgradeLevel( UpgradeType.Autoball, 4 );
		player.AdjustUpgradeLevel( UpgradeType.MoveSpeed, 4 );
		//player.AdjustUpgradeLevel( UpgradeType.Dash, 6 );
		//player.AdjustUpgradeLevel( UpgradeType.Blink, 2 );
		player.AdjustUpgradeLevel( UpgradeType.BumpStrength, 2 );

		//if ( channel.IsHost )
		//{
		//	CopterGameManager.Instance.HostConnected();
		//}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		//DebugDisplay();

		var targetingTrace = Scene.Trace.Ray( Camera.ScreenPixelToRay( Mouse.Position ), 1000f ).Run();
		if ( targetingTrace.Hit )
		{
			MouseWorldPos = (Vector2)targetingTrace.HitPosition;
		}

		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"CurrentScore: {CurrentScore}", new global::Transform( Vector3.Zero ) );

		SlidingGround.Transform.Position = new Vector3( CenterLineOffset, 0f, 0f );

		if ( IsSlowmo )
		{
			if ( _realTimeSinceSlowmoStarted > _slowmoTime )
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

		switch ( GamePhase )
		{
			case GamePhase.WaitingForPlayers:
				break;
			case GamePhase.StartingNewMatch:
				var numSecondsLeft = MathX.CeilToInt( START_NEW_MATCH_DELAY - TimeSincePhaseChange );
				if ( numSecondsLeft != _numSecondsLeftInPhase )
				{
					_numSecondsLeftInPhase = numSecondsLeft;
					var sfx = Sound.Play( "woody_beep" );
					if ( sfx != null )
						sfx.Pitch = numSecondsLeft == 0 ? 1.1f : Utils.Map( numSecondsLeft, (int)START_NEW_MATCH_DELAY - 1, 1, 1f, 0.9f, EasingType.QuadIn );
				}
				break;
			case GamePhase.RoundActive:
				// pickups
				if ( _timeSincePickupSpawn > _pickupSpawnDelay )
				{
					var connection = GetConnection( playerNum: Game.Random.Int( 0, 1 ) );
					if ( connection != null )
					{
						if ( Game.Random.Float( 0f, 1f ) < 0.55f )
						{
							SpawnPickupItem( connection, GetRandomPickupType(), 1, startAtTop: Game.Random.Int( 0, 1 ) == 0 );
						}
						else
						{
							SpawnMoneyPickup( connection, Game.Random.Int( 1, 4 ), startAtTop: Game.Random.Int( 0, 1 ) == 0 );
						}

						_timeSincePickupSpawn = 0f;
						_pickupSpawnDelay = Game.Random.Float( 6.5f, 32f ) * Utils.Map( TimeSincePhaseChange, 0f, 320f, 1f, 0.4f, EasingType.SineIn );
					}
				}

				break;
			case GamePhase.AfterRoundDelay:
				break;
			case GamePhase.BuyPhase:
				var numSecondsLeftBuyPhase = MathX.CeilToInt( BuyPhaseDuration - TimeSincePhaseChange );
				if ( numSecondsLeftBuyPhase <= 3 && numSecondsLeftBuyPhase != _numSecondsLeftInPhase )
				{
					_numSecondsLeftInPhase = numSecondsLeftBuyPhase;
					var sfx = Sound.Play( "woody_beep" );
					if ( sfx != null )
						sfx.Pitch = numSecondsLeftBuyPhase == 0 ? 1.1f : Utils.Map( numSecondsLeftBuyPhase, 3, 1, 1f, 0.9f, EasingType.QuadIn );
				}
				break;
			case GamePhase.Victory:
				break;
		}

		if ( IsProxy )
			return;

		switch ( GamePhase )
		{
			case GamePhase.WaitingForPlayers:
				if ( DoesPlayerExist0 && DoesPlayerExist1 )
					StartNewMatch();
				break;
			case GamePhase.StartingNewMatch:
				if ( TimeSincePhaseChange > START_NEW_MATCH_DELAY )
					StartNewRound();
				break;
			case GamePhase.RoundActive:
				break;
			case GamePhase.AfterRoundDelay:
				if ( !_hasIncrementedScore && TimeSincePhaseChange > BETWEEN_ROUNDS_DELAY / 2f )
				{
					ChangeScore( _roundWinnerPlayerNum );
					_targetCenterLineOffset = Utils.Map( CurrentScore, -SCORE_NEEDED_TO_WIN, SCORE_NEEDED_TO_WIN, 95f, -95f );
					_hasIncrementedScore = true;

					DestroyPickups();

					if ( Math.Abs( CurrentScore ) < SCORE_NEEDED_TO_WIN )
					{
						SpawnScoreText( _roundWinnerPlayerNum, CurrentScore );

						var winningPlayer = GetPlayer( _roundWinnerPlayerNum );
						if ( winningPlayer != null )
							SpawnMoneyPickup( GetConnection( winningPlayer.PlayerNum ), numLevels: 10, new Vector2( CenterLineOffset, 130f ), new Vector2( 128f * (winningPlayer.PlayerNum == 0 ? -1f : 1f) + Game.Random.Float( -5f, 5f ), Game.Random.Float( -64f, 15f ) ), time: Game.Random.Float( 0.6f, 0.85f ) );

						var deadPlayer = GetPlayer( Globals.GetOpponentPlayerNum( _roundWinnerPlayerNum ) );
						if ( deadPlayer != null )
							SpawnMoneyPickup( GetConnection( deadPlayer.PlayerNum ), numLevels: 5, new Vector2( CenterLineOffset, 130f ), new Vector2( 128f * (deadPlayer.PlayerNum == 0 ? -1f : 1f) + Game.Random.Float( -5f, 5f ), Game.Random.Float( -64f, 15f ) ), time: Game.Random.Float( 0.6f, 0.85f ) );
					}
				}

				if ( TimeSincePhaseChange > BETWEEN_ROUNDS_DELAY )
				{
					if ( DoesPlayerExist0 && CurrentScore >= SCORE_NEEDED_TO_WIN )
						Victory( winningPlayerNum: 0 );
					else if ( DoesPlayerExist1 && CurrentScore <= -SCORE_NEEDED_TO_WIN )
						Victory( winningPlayerNum: 1 );
					else
						StartBuyPhase();
				}
				break;
			case GamePhase.BuyPhase:
				if ( TimeSincePhaseChange > BuyPhaseDuration )
					FinishBuyPhase();
				break;
			case GamePhase.Victory:
				if ( TimeSincePhaseChange > VICTORY_DELAY )
					StartNewMatch();
				break;
		}

		//CurrentScore = 4;
		//_targetCenterLineOffset = Utils.Map( CurrentScore, -SCORE_NEEDED_TO_WIN, SCORE_NEEDED_TO_WIN, 95f, -95f );

		CenterLineOffset = Utils.DynamicEaseTo( CenterLineOffset, _targetCenterLineOffset, 0.05f, Time.Delta );
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
		Gizmo.Draw.ScreenText( str, new Vector2( 5f, 30f ), size: 12, flags: TextFlag.Left );
	}

	[Broadcast]
	public void PlayerDied( Guid id )
	{
		Slowmo( 0.125f, 2f, EasingType.SineOut );

		TimeSincePhaseChange = 0f;

		if ( IsProxy )
			return;

		var playerObj = Scene.Directory.FindByGuid( id );
		if ( playerObj != null )
		{
			var deadPlayer = playerObj.Components.Get<PlayerController>();
			_roundWinnerPlayerNum = (deadPlayer.PlayerNum == 0 ? 1 : 0);
		}

		FinishRound();
	}

	void ChangeScore( int winningPlayerNum )
	{
		if ( winningPlayerNum == 0 )
			CurrentScore = Math.Min( CurrentScore + 1, SCORE_NEEDED_TO_WIN );
		else
			CurrentScore = Math.Max( CurrentScore - 1, -SCORE_NEEDED_TO_WIN );
	}

	void StartNewMatch()
	{
		RoundNum = 0;
		Player0?.ClearStats();
		Player1?.ClearStats();
		_targetCenterLineOffset = 0f;
		CurrentScore = 0;

		GamePhase = GamePhase.StartingNewMatch;
		TimeSincePhaseChange = 0f;
		_numSecondsLeftInPhase = (int)START_NEW_MATCH_DELAY;

		TimeScale = 1f;

		SpawnTutorialText();
	}

	void StartNewRound()
	{
		RoundNum++;
		GamePhase = GamePhase.RoundActive;
		TimeSincePhaseChange = 0f;
		_timeSincePickupSpawn = 0f;
		_pickupSpawnDelay = Game.Random.Float( 2.5f, 20f );

		Player0?.ResetRerollPrice();
		Player1?.ResetRerollPrice();

		Dispenser.StartWave();
	}

	void FinishRound()
	{
		DespawnBalls();
		GamePhase = GamePhase.AfterRoundDelay;
		_hasIncrementedScore = false;
	}

	void Victory( int winningPlayerNum )
	{
		PlayVictoryEffects();

		Player0?.Respawn();
		Player1?.Respawn();

		if ( DoesPlayerExist0 )
		{
			if ( winningPlayerNum == 0 )
				Player0.AddMatchVictory();
			else
				Player0.AddMatchLoss();
		}

		if ( DoesPlayerExist1 )
		{
			if ( winningPlayerNum == 1 )
				Player1.AddMatchVictory();
			else
				Player1.AddMatchLoss();
		}

		GamePhase = GamePhase.Victory;
		WinningPlayerName = GetPlayer( winningPlayerNum ).GameObject.Network.OwnerConnection.DisplayName;
	}

	[Broadcast]
	public void PlayVictoryEffects()
	{
		Sound.Play( "victory" );
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
			CreateRerollButton( 0 );
			CreateShopItems( playerNum: 0, numItems: Player0.NumShopItems );
		}

		if ( DoesPlayerExist1 )
		{
			CreateSkipButton( 1 );
			CreateRerollButton( 1 );
			CreateShopItems( playerNum: 1, numItems: Player1.NumShopItems );
		}
	}

	void CreateShopItems( int playerNum, int numItems )
	{
		for ( int i = 0; i < numItems; i++ )
		{
			var rarity = GetRandomRarity();
			var upgradeType = GetRandomUpgradeType( rarity );
			int numLevels = GetRandomAmountForUpgrade( upgradeType );
			int pricePerLevel = GetRandomPricePerAmountForUpgrade( upgradeType );
			var price = numLevels * pricePerLevel;

			// bulk discount
			if ( numLevels > 1 )
				price = Math.Max( price - Game.Random.Int( 1, numLevels - 1 ), 1 );

			CreateShopItem( playerNum, i, upgradeType, numLevels, price );
		}
	}

	void FinishBuyPhase()
	{
		DestroyShopStuff();

		StartNewRound();
	}

	void CreateSkipButton( int playerNum )
	{
		var skipButtonObj = SkipButtonPrefab.Clone( new Vector3( 212f * (playerNum == 0 ? -1f : 1f), 103f, 0f ) );
		skipButtonObj.NetworkSpawn( GetConnection( playerNum ) );
	}

	void CreateRerollButton( int playerNum )
	{
		var rerollButtonObj = RerollButtonPrefab.Clone( new Vector3( 114f * (playerNum == 0 ? -1f : 1f), 103f, 0f ) );
		rerollButtonObj.NetworkSpawn( GetConnection( playerNum ) );
		rerollButtonObj.Components.Get<RerollButton>().Init( playerNum );
	}

	void CreateShopItem( int playerNum, int itemNum, UpgradeType upgradeType, int numLevels, int price )
	{
		var pos = GetPosForShopItem( playerNum, itemNum );

		var shopItemObj = IsUpgradePassive( upgradeType )
			? ShopItemPassivePrefab.Clone( new Vector3( pos.x, pos.y, 0f ) )
			: ShopItemPrefab.Clone( new Vector3( pos.x, pos.y, 0f ) );

		shopItemObj.NetworkSpawn( GetConnection( playerNum ) );
		shopItemObj.Components.Get<ShopItem>().Init( upgradeType, numLevels, price, playerNum );
	}

	Vector2 GetPosForShopItem( int playerNum, int itemNum )
	{
		var topPos = new Vector2( -215f * (playerNum == 0 ? 1f : -1f), 65f );
		var interval = 41f;

		Vector2 offset;

		if ( itemNum < 5 )
			offset = new Vector2( 0f, -itemNum * interval );
		else
			offset = new Vector2( (itemNum - 4) * interval * (playerNum == 0 ? 1f : -1f), -4 * interval );

		return topPos + offset;
	}

	public void SpawnPickupItem( Connection connection, UpgradeType upgradeType, int numLevels, bool startAtTop )
	{
		var pos = new Vector2( 0f, 150f * (startAtTop ? 1f : -1f) );

		var pickupItemObj = PickupItemPrefab.Clone( new Vector3( pos.x, pos.y, 120f * (startAtTop ? 1f : -1f) ) );
		pickupItemObj.NetworkSpawn( connection );
		pickupItemObj.Components.Get<PickupItem>().Init( upgradeType, numLevels, startAtTop );
	}

	public void SpawnMoneyPickup( Connection connection, int numLevels, bool startAtTop )
	{
		var pos = new Vector2( 0f, 150f * (startAtTop ? 1f : -1f) );

		var moneyPickupObj = MoneyPickupPrefab.Clone( new Vector3( pos.x, pos.y, 0f ) );
		moneyPickupObj.NetworkSpawn( connection );
		moneyPickupObj.Components.Get<MoneyPickup>().Init( numLevels, startAtTop );
	}

	public void SpawnMoneyPickup( Connection connection, int numLevels, Vector2 startPos, Vector2 endPos, float time )
	{
		var moneyPickupObj = MoneyPickupPrefab.Clone( new Vector3( startPos.x, startPos.y, 0f ) );
		moneyPickupObj.NetworkSpawn( connection );
		moneyPickupObj.Components.Get<MoneyPickup>().Init( numLevels, startPos, endPos, time );
	}

	[Broadcast]
	public void SkipButtonHit()
	{
		if ( IsProxy || GamePhase != GamePhase.BuyPhase )
			return;

		_numBuyPhaseSkips++;

		if ( _numBuyPhaseSkips >= NumActivePlayers )
		{
			FinishBuyPhase();
		}
		else
		{
			foreach ( var skipButton in Scene.GetAllComponents<SkipButton>() )
				skipButton.StartFlashing();
		}
	}

	[Broadcast]
	public void RerollButtonHit( int playerNum )
	{
		if ( IsProxy || GamePhase != GamePhase.BuyPhase )
			return;

		DestroyShopItems( playerNum );

		var player = GetPlayer( playerNum );
		if ( player == null )
			return;

		player.IncreaseRerollPrice();

		CreateShopItems( playerNum, numItems: player.NumShopItems );
		RespawnRerollButton( playerNum );
	}

	async void RespawnRerollButton( int playerNum )
	{
		await Task.Delay( 1000 );

		if ( GamePhase == GamePhase.BuyPhase )
			CreateRerollButton( playerNum );
	}

	void DestroyShopItems( int playerNum )
	{
		foreach ( var shopItem in Scene.GetAllComponents<ShopItem>() )
		{
			if ( (playerNum == 0 && shopItem.Transform.Position.x < 0f) || (playerNum == 1 && shopItem.Transform.Position.x > 0f) )
				shopItem.DestroyButton();
		}
	}

	public void SpawnBall( Vector2 pos, Vector2 velocity, int playerNum, float radius )
	{
		var height = (playerNum == 0 && pos.x > CenterLineOffset || playerNum == 1 && pos.x < CenterLineOffset) ? BALL_HEIGHT_OPPONENT : BALL_HEIGHT_SELF;
		var ballObj = BallPrefab.Clone( new Vector3( pos.x, pos.y, height ) );
		var ball = ballObj.Components.Get<Ball>();

		ball.Velocity = velocity;
		ball.SetRadius( radius );

		//Log.Info( $"SpawnBall - connection: {connection}" );

		//ballObj.NetworkSpawn( GetConnection( playerNum ) );
		//ballObj.NetworkSpawn();

		ball.Color = (playerNum == 0 ? ColorPlayer0 : ColorPlayer1);
		ball.PlayerNum = playerNum;

		int side = pos.x > 0f ? 1 : 0;
		ball.CurrentSide = side;

		ballObj.Network.SetOwnerTransfer( OwnerTransfer.Takeover );
		ballObj.Network.SetOrphanedMode( NetworkOrphaned.Destroy );

		var connection = GetConnection( side );
		if ( connection != null )
		{
			ballObj.NetworkSpawn( connection );
		}
		else
		{
			ballObj.NetworkSpawn();
		}

		//Log.Info( $"--- spawned {ballObj.Name} side: {side} connection: {ballObj.Network.OwnerConnection?.Id.ToString().Substring( 0, 6 ) ?? "..."}" );
	}

	[Broadcast]
	public void SetPlayerActive( int playerNum, Guid id )
	{
		if ( IsProxy )
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
		player.PassiveUpgrades.Clear();
		player.ActiveUpgrades.Clear();
		player.SelectedUpgradeType = UpgradeType.None;

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
		Slowmo( 0.025f, 1f, EasingType.SineOut );

		if ( IsProxy )
			return;
	}

	public Connection GetConnection( int playerNum )
	{
		if ( playerNum == 0 && DoesPlayerExist0 )
			return Scene.Directory.FindByGuid( PlayerId0 ).Network.OwnerConnection;
		else if ( playerNum == 1 && DoesPlayerExist1 )
			return Scene.Directory.FindByGuid( PlayerId1 ).Network.OwnerConnection;

		return null;
	}

	public PlayerController GetPlayer( int playerNum )
	{
		if ( playerNum == 0 && DoesPlayerExist0 )
			return Scene.Directory.FindByGuid( PlayerId0 ).Components.Get<PlayerController>();
		else if ( playerNum == 1 && DoesPlayerExist1 )
			return Scene.Directory.FindByGuid( PlayerId1 ).Components.Get<PlayerController>();

		return null;
	}

	public static int GetOtherPlayerNum( int playerNum )
	{
		return playerNum == 0 ? 1 : 0;
	}

	public void OnDisconnected( Connection channel )
	{
		if ( IsProxy )
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
		else if ( DoesPlayerExist1 && channel == GetConnection( 1 ) )
		{
			DoesPlayerExist1 = false;
			Player1 = null;
			PlayerId1 = Guid.Empty;
			activePlayerLeft = true;
		}

		if ( activePlayerLeft )
		{
			StopCurrentMatch();
		}

		Log.Info( $"Player1: {Player1}, (1GetConnection): {GetConnection( 1 )}, channel 1? {(GetConnection( 1 ) == channel)}" );

		// check if active players still exist, and stop match if one of them left
		// if spectators exist, fill the spot with one of them who has played the least matches
	}

	void StopCurrentMatch()
	{
		//Log.Info( $"StopCurrentMatch" );
		_targetCenterLineOffset = 0f;
		CurrentScore = 0;

		Player0?.Respawn();
		Player1?.Respawn();

		Player0?.ClearStats();
		Player1?.ClearStats();

		DespawnBalls();
		DestroyShopStuff();
		DestroyPickups();
		DestroyFadingText();

		GamePhase = GamePhase.WaitingForPlayers;
	}

	[Broadcast]
	public void SlowmoRPC( float timeScale, float time, EasingType easingType )
	{
		Slowmo( timeScale, time, easingType );
	}

	public void Slowmo( float timeScale, float time, EasingType easingType )
	{
		IsSlowmo = true;
		Scene.TimeScale = timeScale;

		_slowmoTime = time;
		_slowmoTimeScale = timeScale;
		_realTimeSinceSlowmoStarted = 0f;
		_slowmoEasingType = easingType;
	}

	void DespawnBalls()
	{
		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( ball.IsActive )
				ball.Despawn();
		}
	}

	//void DestroyBalls()
	//{
	//	foreach ( var ball in Scene.GetAllComponents<Ball>() )
	//	{
	//		ball.DestroyBall();
	//	}
	//}

	void DestroyPickups()
	{
		foreach ( var pickup in Scene.GetAllComponents<PickupItem>() )
			pickup.DestroyRPC();

		foreach ( var pickup in Scene.GetAllComponents<MoneyPickup>() )
			pickup.DestroyRPC();
	}

	[Broadcast]
	void DestroyFadingText()
	{
		foreach ( var fadingText in Scene.GetAllComponents<FadingText>() )
			fadingText.GameObject.Destroy();
	}

	void DestroyShopStuff()
	{
		foreach ( var skipButton in Scene.GetAllComponents<SkipButton>() )
			skipButton.DestroyButton();

		foreach ( var rerollButton in Scene.GetAllComponents<RerollButton>() )
			rerollButton.DestroyButton();

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

		var otherPlayer = GetPlayer( Globals.GetOpponentPlayerNum( player.PlayerNum ) );
		if(otherPlayer != null)
		{
			if( (otherPlayer.PlayerNum == 0 && CurrentScore > 0) || (otherPlayer.PlayerNum == 1 && CurrentScore < 0))
				player.AddMatchLoss();
		}

		if ( DoesPlayerExist0 && PlayerId0 == id )
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

	[Broadcast]
	public void SpawnTutorialText()
	{
		SpawnTutorialTextAsync();
	}

	async void SpawnTutorialTextAsync()
	{
		string blue_circle = "🔵";
		string green_circle = "🟢";

		await Task.Delay( 400 );

		if ( GamePhase == GamePhase.WaitingForPlayers )
			return;

		var localPlayer = GetLocalPlayer();
		if ( localPlayer == null || localPlayer.IsSpectator )
			return;

		if( localPlayer.PlayerNum == 0)
			SpawnFadingText( new Vector3( -120f, 40f, 180f ), $"AVOID {green_circle}", 4f );
		else
			SpawnFadingText( new Vector3( 120f, 40f, 180f ), $"AVOID {blue_circle}", 3f );

		await Task.Delay( 1200 );

		if ( GamePhase == GamePhase.WaitingForPlayers )
			return;

		if ( localPlayer.PlayerNum == 0 )
			SpawnFadingText( new Vector3( -120f, -10f, 180f ), $"BUMP {blue_circle}", 4f );
		else
			SpawnFadingText( new Vector3( 120f, -10f, 180f ), $"BUMP {green_circle}", 3f );
	}

	[Broadcast]
	public void SpawnScoreText( int winningPlayerNum, int currentScore )
	{
		var connection = GetConnection( winningPlayerNum );
		string name = connection != null ? $"{connection.DisplayName}" : (winningPlayerNum == 0 ? "🟦" : "🟩");
		//int amountNeeded = SCORE_NEEDED_TO_WIN + CurrentScore * (winningPlayerNum == 0 ? -1 : 1);

		SpawnFadingText( new Vector3( 0f, 20f, 180f ), $"{name} WON THE ROUND", 3f );
	}

	public void SpawnFadingText(Vector3 pos, string text, float lifetime )
	{
		var textObj = FadingTextPrefab.Clone( pos );
		textObj.Transform.Rotation = Rotation.From( 90f, 90f, 0f );

		textObj.Components.Get<FadingText>().Init( text, lifetime );
	}

	public void SpawnFloaterText( Vector3 pos, string text, float lifetime, Color color, Vector2 velocity, float deceleration, float startScale, float endScale, bool isEmoji  )
	{
		var textObj = FloaterTextPrefab.Clone( pos );
		textObj.Transform.Rotation = Rotation.From( 90f, 90f, 0f );

		textObj.Components.Get<FloaterText>().Init( text, lifetime, color, velocity, deceleration, startScale, endScale, isEmoji );
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

	[Broadcast]
	public void PlaySfx( string name, Vector3 pos )
	{
		Sound.Play( name, pos );
	}

	[Broadcast]
	public void PlaySfx( string name, Vector3 pos, float pitch )
	{
		var sfx = Sound.Play( name, pos );
		if ( sfx != null )
			sfx.Pitch = pitch;
	}

	public string GetNameForUpgrade( UpgradeType upgradeType )
	{
		if ( UpgradeDatas.ContainsKey( upgradeType ) )
			return UpgradeDatas[upgradeType].name;

		return "";
	}

	public string GetIconForUpgrade( UpgradeType upgradeType )
	{
		if ( UpgradeDatas.ContainsKey( upgradeType ) )
			return UpgradeDatas[upgradeType].icon;

		return "";
	}

	public string GetDescriptionForUpgrade( UpgradeType upgradeType, int level )
	{
		switch(upgradeType)
		{
			case UpgradeType.MoveSpeed: return $"Move {Math.Floor((MoveSpeedUpgrade.GetIncrease(level) - 1f) * 100f)}% faster";
			case UpgradeType.BumpStrength: return $"Bumping speeds up balls by {Math.Round( BumpStrengthUpgrade.GetIncrease( level ) )}";
			case UpgradeType.Autoball: return $"Release a ball every {(float)Math.Round( AutoballUpgrade.GetDelay( level ), 1 )}s";

			case UpgradeType.Volley: return $"Shoot some balls forward";
			case UpgradeType.Gather: return $"Your balls target you";
			case UpgradeType.Repel: return $"Push nearby balls away";
			case UpgradeType.Blink: return $"Teleport to your cursor";
			case UpgradeType.Scatter: return $"Redirect all balls randomly";
			case UpgradeType.Slowmo: return $"Briefly slow time";
			case UpgradeType.Dash: return $"Move forward quicky";
			case UpgradeType.Redirect: return $"All your balls move in the direction from you to cursor";
			case UpgradeType.Converge: return $"Your balls target enemy";
		}

		return "";
	}

	public UpgradeRarity GetRarityForUpgrade( UpgradeType upgradeType )
	{
		if ( UpgradeDatas.ContainsKey( upgradeType ) )
			return UpgradeDatas[upgradeType].rarity;

		return UpgradeRarity.Common;
	}

	public int GetMaxLevelForUpgrade( UpgradeType upgradeType )
	{
		if ( UpgradeDatas.ContainsKey( upgradeType ) )
			return UpgradeDatas[upgradeType].maxLevel;

		return 0;
	}

	public int GetRandomAmountForUpgrade( UpgradeType upgradeType )
	{
		if ( UpgradeDatas.ContainsKey( upgradeType ) )
			return Game.Random.Int(UpgradeDatas[upgradeType].amountMin, UpgradeDatas[upgradeType].amountMax);

		return 0;
	}

	public int GetRandomPricePerAmountForUpgrade( UpgradeType upgradeType )
	{
		if ( UpgradeDatas.ContainsKey( upgradeType ) )
			return Game.Random.Int( UpgradeDatas[upgradeType].pricePerAmountMin, UpgradeDatas[upgradeType].pricePerAmountMax );

		return 0;
	}

	public bool IsUpgradePassive( UpgradeType upgradeType )
	{
		if ( UpgradeDatas.ContainsKey( upgradeType ) )
			return UpgradeDatas[upgradeType].isPassive;

		return true;
	}

	public bool CanUseUpgradeInBuyPhase( UpgradeType upgradeType )
	{
		if ( UpgradeDatas.ContainsKey( upgradeType ) )
			return UpgradeDatas[upgradeType].useableInBuyPhase;

		return false;
	}

	public static Color GetColorForRarity( UpgradeRarity rarity )
	{
		switch ( rarity )
		{
			case UpgradeRarity.Common: default: return new Color( 0f, 0f, 0f );
			case UpgradeRarity.Uncommon: return new Color( 0.5f, 0.5f, 0.8f );
			case UpgradeRarity.Rare: return new Color( 0.8f, 0.3f, 0f );
			case UpgradeRarity.Epic: return new Color( 0.6f, 0f, 0.8f );
			case UpgradeRarity.Legendary: return new Color( 1f, 1f, 0f );
		}
	}

	public static int GetOutlineSizeForRarity( UpgradeRarity rarity )
	{
		switch ( rarity )
		{
			case UpgradeRarity.Common: default: return 6;
			case UpgradeRarity.Uncommon: return 12;
			case UpgradeRarity.Rare: return 12;
			case UpgradeRarity.Epic: return 12;
			case UpgradeRarity.Legendary: return 12;
		}
	}

	void GenerateUpgrades()
	{
		CreateUpgrade( UpgradeType.MoveSpeed, "Cardio", "🏃🏻", UpgradeRarity.Common, maxLevel: 9, amountMin: 1, amountMax: 1, pricePerAmountMin: 3, pricePerAmountMax: 5, isPassive: true );
		CreateUpgrade( UpgradeType.BumpStrength, "Muscles", "💪", UpgradeRarity.Uncommon, maxLevel: 9, amountMin: 1, amountMax: 1, pricePerAmountMin: 3, pricePerAmountMax: 6, isPassive: true );
		CreateUpgrade( UpgradeType.Autoball, "Autoball", "⏲️", UpgradeRarity.Rare, maxLevel: 9, amountMin: 1, amountMax: 1, pricePerAmountMin: 5, pricePerAmountMax: 7, isPassive: true );

		CreateUpgrade( UpgradeType.Volley, "Volley", "🔴", UpgradeRarity.Common, maxLevel: 5, amountMin: 1, amountMax: 2, pricePerAmountMin: 3, pricePerAmountMax: 5 );
		CreateUpgrade( UpgradeType.Gather, "Gather", "🧲", UpgradeRarity.Uncommon, maxLevel: 9, amountMin: 1, amountMax: 1, pricePerAmountMin: 3, pricePerAmountMax: 5 );
		CreateUpgrade( UpgradeType.Repel, "Repel", "💥", UpgradeRarity.Common, maxLevel: 9, amountMin: 1, amountMax: 2, pricePerAmountMin: 2, pricePerAmountMax: 5 );
		CreateUpgrade( UpgradeType.Replace, "Replace", "☯️", UpgradeRarity.Uncommon, maxLevel: 3, amountMin: 1, amountMax: 1, pricePerAmountMin: 6, pricePerAmountMax: 8 );
		CreateUpgrade( UpgradeType.Blink, "Blink", "✨", UpgradeRarity.Uncommon, maxLevel: 9, amountMin: 1, amountMax: 2, pricePerAmountMin: 2, pricePerAmountMax: 4, useableInBuyPhase: true );
		CreateUpgrade( UpgradeType.Scatter, "Scatter", "🌪️", UpgradeRarity.Uncommon, amountMin: 1, amountMax: 2, pricePerAmountMin: 2, pricePerAmountMax: 5, maxLevel: 3 );
		CreateUpgrade( UpgradeType.Slowmo, "Slowmo", "⌛️", UpgradeRarity.Common, maxLevel: 9, amountMin: 1, amountMax: 2, pricePerAmountMin: 1, pricePerAmountMax: 3 );
		CreateUpgrade( UpgradeType.Dash, "Dash", "💨", UpgradeRarity.Common, maxLevel: 9, amountMin: 1, amountMax: 3, pricePerAmountMin: 1, pricePerAmountMax: 2, useableInBuyPhase: true );
		CreateUpgrade( UpgradeType.Redirect, "Redirect", "⤴️", UpgradeRarity.Rare, maxLevel: 3, amountMin: 1, amountMax: 1, pricePerAmountMin: 5, pricePerAmountMax: 7 );
		CreateUpgrade( UpgradeType.Converge, "Converge", "📍", UpgradeRarity.Epic, maxLevel: 3, amountMin: 1, amountMax: 1, pricePerAmountMin: 4, pricePerAmountMax: 6 );

		foreach (var upgradeData in UpgradeDatas)
		{
			var upgradeType = upgradeData.Key;
			var rarity = upgradeData.Value.rarity;
			if ( UpgradesByRarity.ContainsKey( rarity ) )
				UpgradesByRarity[rarity].Add( upgradeType );
			else
				UpgradesByRarity.Add(rarity, new List<UpgradeType> { upgradeType } );
		}
	}

	public UpgradeType GetRandomUpgradeType()
	{
		return (UpgradeType)Game.Random.Int( 1, Enum.GetValues( typeof( UpgradeType ) ).Length - 1 );
	}

	public UpgradeType GetRandomUpgradeType(UpgradeRarity rarity)
	{
		if ( UpgradesByRarity.ContainsKey( rarity ) && UpgradesByRarity[rarity].Count > 0 )
			return UpgradesByRarity[rarity][Game.Random.Int( 0, UpgradesByRarity[rarity].Count - 1 )];

		return UpgradeType.None;
	}

	public UpgradeRarity GetRandomRarity()
	{
		Dictionary<UpgradeRarity, float> weights = new Dictionary<UpgradeRarity, float>
		{
			{ UpgradeRarity.Common, 100f },
			{ UpgradeRarity.Uncommon, 58f },
			{ UpgradeRarity.Rare, 27f },
			//{ UpgradeRarity.Epic, 16f },
			//{ UpgradeRarity.Legendary, 2f },
		};

		var total = 0f;
		foreach ( var weight in weights.Values )
			total += weight;

		float rand = Game.Random.Float( 0f, total );

		var runningTotal = 0f;
		foreach( var pair in weights )
		{
			runningTotal += pair.Value;
			var rarity = pair.Key;
			if ( rand < runningTotal && UpgradesByRarity.ContainsKey( rarity ) )
				return rarity;
		}

		return UpgradeRarity.Common;
	}

	void CreateUpgrade(UpgradeType upgradeType, string name, string icon, UpgradeRarity rarity, int maxLevel, 
		int amountMin, int amountMax, int pricePerAmountMin, int pricePerAmountMax,
		bool isPassive = false, bool useableInBuyPhase = false)
	{
		UpgradeDatas.Add(upgradeType, new UpgradeData(name, icon, rarity, maxLevel, amountMin, amountMax, pricePerAmountMin, pricePerAmountMax, isPassive, useableInBuyPhase));
	}

	UpgradeType GetRandomPickupType()
	{
		Dictionary<UpgradeType, float> weights = new Dictionary<UpgradeType, float>
		{
			{ UpgradeType.Blink, 10f },
			{ UpgradeType.Dash, 12f },
			{ UpgradeType.Volley, 4f },
			{ UpgradeType.Repel, 5f },
			{ UpgradeType.Gather, 3f },
			{ UpgradeType.Slowmo, 5f },
		};

		var total = 0f;
		foreach ( var weight in weights.Values )
			total += weight;

		float rand = Game.Random.Float( 0f, total );

		var runningTotal = 0f;
		foreach ( var pair in weights )
		{
			runningTotal += pair.Value;
			var upgradeType = pair.Key;
			if ( rand < runningTotal )
				return upgradeType;
		}

		return UpgradeType.Dash;
	}
}
