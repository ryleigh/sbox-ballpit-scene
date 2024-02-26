using Sandbox;
using Sandbox.Network;
using System.Diagnostics.Metrics;
using System.IO;
using System.Numerics;
using System.Threading.Channels;

public sealed class Manager : Component, Component.INetworkListener
{
	public static Manager Instance { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject BallPrefab { get; set; }

	[Property, Sync] public Color ColorPlayer0 { get; set; }
	[Property, Sync] public Color ColorPlayer1 { get; set; }

	public const float X_FAR = 228f;
	public const float X_CLOSE = 21f;

	public const float Y_LIMIT = 110.3f;

	public const float BALL_HEIGHT_SELF = 45f;
	public const float BALL_HEIGHT_OPPONENT = 55f;

	public PlayerController Player0 { get; set; }
	public PlayerController Player1 { get; set; }
	[Sync] public Guid PlayerId0 { get; set; }
	[Sync] public Guid PlayerId1 { get; set; }
	[Sync] public bool DoesPlayerExist0 { get; set; }
	[Sync] public bool DoesPlayerExist1 { get; set; }

	[Sync] public int RoundNum { get; private set; }

	[Sync] public bool IsRoundActive { get; private set; }
	private TimeSince _timeSinceRoundFinished;

	public Vector3 OriginalCameraPos { get; private set; }
	public Rotation OriginalCameraRot { get; private set; }

	public Dispenser Dispenser { get; private set; }

	public bool IsSlowmo { get; set; }
	private float _slowmoTime;
	private float _slowmoTimeScale;
	private RealTimeSince _realTimeSinceSlowmoStarted;
	private EasingType _slowmoEasingType;

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

		IsRoundActive = true;
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
			SetPlayer( 0, player );
			player.PlayerNum = 0;
		}
		else if ( !DoesPlayerExist1 )
		{
			SetPlayer( 1, player );
			player.PlayerNum = 1;
		}
		else
		{
			player.IsSpectator = true;
		}

		playerObj.NetworkSpawn( channel );

		//if ( channel.IsHost )
		//{
		//	CopterGameManager.Instance.HostConnected();
		//}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		DebugDisplay();

		if(IsSlowmo)
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
		
		if (!IsRoundActive && _timeSinceRoundFinished > 4f)
		{
			StartNewRound();
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

		if ( IsProxy )
			return;

		var playerObj = Scene.Directory.FindByGuid( id );
		if(playerObj != null)
		{
			var player = playerObj.Components.Get<PlayerController>();
			player.AddScoreAndMoney( score: 0, money: 5 );

			var otherPlayer = GetPlayer( GetOtherPlayerNum( player.PlayerNum ) );
			if ( otherPlayer != null )
				otherPlayer.AddScoreAndMoney(score: 1, money: 10);
		}

		IsRoundActive = false;
		_timeSinceRoundFinished = 0f;

		foreach(var ball in Scene.GetAllComponents<Ball>())
		{
			if ( ball.IsActive )
				ball.Despawn();
		}
	}

	void StartNewRound()
	{
		RoundNum++;
		IsRoundActive = true;
		Dispenser.StartWave();

		Player0?.Respawn();
		Player1?.Respawn();
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

	void SetPlayer(int playerNum, PlayerController player )
	{
		if (playerNum == 0 )
		{
			Player0 = player;
			PlayerId0 = player.GameObject.Id;
			DoesPlayerExist0 = true;
			//Log.Info( $"Setting player 0: {player.GameObject.Id}" );
		}
		else if(playerNum == 1)
		{
			Player1 = player;
			PlayerId1 = player.GameObject.Id;
			DoesPlayerExist1 = true;
			//Log.Info( $"Setting player 1: {player.GameObject.Id}" );
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
		Log.Info( $"OnDisconnected: {channel.DisplayName}" );
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
}
