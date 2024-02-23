using Sandbox;
using Sandbox.Network;
using System.Diagnostics.Metrics;
using System.Threading.Channels;

public sealed class Manager : Component, Component.INetworkListener
{
	public static Manager Instance { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject BallPrefab { get; set; }

	[Property] public Color ColorPlayer0 { get; set; }
	[Property] public Color ColorPlayer1 { get; set; }

	public const float X_FAR = 228f;
	public const float X_CLOSE = 21f;

	public const float Y_LIMIT = 110.3f;

	public const float BALL_HEIGHT = 50f;

	[Sync] public Guid PlayerId0 { get; set; }
	[Sync] public Guid PlayerId1 { get; set; }
	[Sync] public bool DoesPlayerExist0 { get; set; }
	[Sync] public bool DoesPlayerExist1 { get; set; }

	[Sync] public int RoundNum { get; private set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;
	}

	protected override void OnStart()
	{
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
		}

		if ( Networking.IsHost )
			Network.TakeOwnership();
	}

	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' is becoming active (local = {channel == Connection.Local}) (host = {channel.IsHost})" );

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
			SetPlayer( 0, playerObj.Id );
			player.PlayerNum = 0;
		}
		else if ( !DoesPlayerExist1 )
		{
			SetPlayer( 1, playerObj.Id );
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

		string str = "";
		foreach(var player in Scene.GetAllComponents<PlayerController>())
		{
			str += $"{player.Network.OwnerConnection.DisplayName}";
			str += $"{(player.Network.IsOwner ? " (local)" : "")}";
			str += $"{(player.Network.OwnerConnection.IsHost ? " (host)" : "")}";
			str += $"{(player.IsDead ? " 💀" : "")}";

			if (DoesPlayerExist0 && player.GameObject.Id == PlayerId0)
				str += $" ..... PLAYER 0";

			if ( DoesPlayerExist1 && player.GameObject.Id == PlayerId1 )
				str += $" ..... PLAYER 1";

			str += $"\n";
		}
		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.ScreenText( str, new Vector2(5f, 5f), size: 12, flags: TextFlag.Left);
	}

	public void SpawnBall(Vector2 pos, Vector2 velocity, int playerNum)
	{
		var ballObj = BallPrefab.Clone( new Vector3(pos.x, pos.y, BALL_HEIGHT ) );
		var ball = ballObj.Components.Get<Ball>();

		ball.Velocity = velocity;

		ball.SetPlayerNum( playerNum );

		//Log.Info( $"SpawnBall - connection: {connection}" );

		ballObj.NetworkSpawn( GetConnection( playerNum ) );

		//int side = pos.x > 0f ? 1 : 0;
		//ballObj.NetworkSpawn(GetConnection(side));
	}

	void SetPlayer(int playerNum, Guid id)
	{
		if (playerNum == 0 )
		{
			PlayerId0 = id;
			DoesPlayerExist0 = true;
			Log.Info( $"Setting player 0: {id}" );
		}
		else if(playerNum == 1)
		{
			PlayerId1 = id;
			DoesPlayerExist1 = true;
			Log.Info( $"Setting player 1: {id}" );
		}
	}

	public Connection GetConnection(int playerNum)
	{
		if( playerNum == 0 && DoesPlayerExist0)
			return Scene.Directory.FindByGuid( PlayerId0 ).Network.OwnerConnection;
		else if(playerNum == 1 && DoesPlayerExist1)
			return Scene.Directory.FindByGuid( PlayerId1 ).Network.OwnerConnection;

		return null;
	}

	public void OnDisconnected( Connection channel )
	{
		Log.Info( $"OnDisconnected: {channel.DisplayName}" );
	}
}
