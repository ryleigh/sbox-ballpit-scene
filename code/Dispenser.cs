using Sandbox;

public sealed class Dispenser : Component
{
	public const float HEIGHT = 50f;
	private Vector3 _topPos;
	private Vector3 _botPos;

	public float Speed { get; private set; }
	public bool IsGoingUp { get; private set; }

	[Sync] public int WaveNum { get; private set; }

	[Sync] public bool IsWaveActive { get; private set; }
	public TimeSince TimeSinceWaveEnded { get; private set; }
	public TimeSince TimeSinceShoot { get; private set; }

	private const float SHOOT_THRESHOLD = 113f;

	public int ShotNum { get; private set; }

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
			return;

		_topPos = new Vector3( 0f, 160f, HEIGHT );
		_botPos = new Vector3( 0f, -160f, HEIGHT );

		IsGoingUp = Game.Random.Int( 0, 1 ) == 0;
		Transform.Position = IsGoingUp ? _botPos : _topPos;

		//StartWave();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if( IsWaveActive )
		{
			if ( Manager.Instance.GamePhase != GamePhase.RoundActive )
				Speed *= (1f + 1.5f * Time.Delta);

			Transform.Position = Transform.Position.WithY( Transform.Position.y + Speed * (IsGoingUp ? 1f : -1f) * Time.Delta );
			var y = Transform.Position.y;

			if( y < SHOOT_THRESHOLD && y > -SHOOT_THRESHOLD && Manager.Instance.GamePhase == GamePhase.RoundActive )
			{
				if ( TimeSinceShoot > 0.25f )
				{
					var speed = 85f;

					Manager.Instance.SpawnBall( (Vector2)Transform.Position, new Vector2( 1f, 0f ) * speed, playerNum: ShotNum % 2 == 0 ? 0 : 1 );
					Manager.Instance.SpawnBall( (Vector2)Transform.Position, new Vector2( -1f, 0f ) * speed, playerNum: ShotNum % 2 == 0 ? 1 : 0 );

					TimeSinceShoot = 0f;
					ShotNum++;
				}
			}

			if((IsGoingUp && y > _topPos.y) || (!IsGoingUp && y < _botPos.y))
			{
				WaveFinished();
			}
		}
		else
		{
			if( TimeSinceWaveEnded > 1f && Manager.Instance.GamePhase == GamePhase.RoundActive )
			{
				StartWave();
			}
		}
	}

	public void StartWave()
	{
		IsGoingUp = !IsGoingUp;
		Transform.Position = IsGoingUp ? _botPos : _topPos;
		IsWaveActive = true;
		TimeSinceShoot = 0f;
		ShotNum = 0;

		Speed = 40f;
	}

	public void WaveFinished()
	{
		IsWaveActive = false;
		TimeSinceWaveEnded = 0f;
	}
}
