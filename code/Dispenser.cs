using Sandbox;

public enum DispenserPattern { Alternate, AlternateDouble, AlternateTriple, FiveToOne, }

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

	private DispenserPattern _dispenserPattern;

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
			return;

		_topPos = new Vector3( 0f, 160f, HEIGHT );
		_botPos = new Vector3( 0f, -160f, HEIGHT );

		IsGoingUp = Game.Random.Int( 0, 1 ) == 0;
		Transform.LocalPosition = IsGoingUp ? _botPos : _topPos;

		//StartWave();
	}

	protected override void OnUpdate()
	{
		//Gizmo.Draw.Color = Color.White;
		//Gizmo.Draw.Text( $"{Manager.Instance.TimeSincePhaseChange}", new global::Transform( Transform.Position ) );

		if ( IsProxy )
			return;

		if( IsWaveActive )
		{
			if ( Manager.Instance.GamePhase != GamePhase.RoundActive )
				Speed *= (1f + 1.5f * Time.Delta);

			Transform.LocalPosition = Transform.LocalPosition.WithY( Transform.LocalPosition.y + Speed * (IsGoingUp ? 1f : -1f) * Time.Delta );
			var y = Transform.LocalPosition.y;

			if( y < SHOOT_THRESHOLD && y > -SHOOT_THRESHOLD && Manager.Instance.GamePhase == GamePhase.RoundActive )
			{
				float delay = 
					Utils.Map( Manager.Instance.RoundNum, 1, 10, 0.2f, 0f, EasingType.SineOut) 
					+ Utils.Map( Manager.Instance.TimeSincePhaseChange, 0f, Utils.Map( Manager.Instance.RoundNum, 1, 8, 60f, 30f ), 0.5f, 0.25f );

				if ( TimeSinceShoot > delay )
				{
					var ballSpeed =
						Utils.Map( Manager.Instance.RoundNum, 1, 16, 75f, 100f, EasingType.SineOut )
						* Utils.Map( Manager.Instance.TimeSincePhaseChange, 0f, Utils.Map( Manager.Instance.RoundNum, 1, 16, 240f, 120f ), 1f, Utils.Map( Manager.Instance.RoundNum, 1, 32, 1.5f, 4f ) )
						* Utils.Map( Manager.Instance.TimeSincePhaseChange, 0f, 600f, 1f, 2f );

					//var speed = 85f;
					//_dispenserPattern = DispenserPattern.FiveToOne;

					int playerNum;
					switch(_dispenserPattern)
					{
						case DispenserPattern.Alternate:
							playerNum = ShotNum % 2 == 0 ? 0 : 1;
							if ( WaveNum % 5 < 2 )
								playerNum = Globals.GetOpponentPlayerNum( playerNum );

							SpawnBall( playerNum, toLeft: true, ballSpeed );
							SpawnBall( Globals.GetOpponentPlayerNum( playerNum ), toLeft: false, ballSpeed );
							break;
						case DispenserPattern.AlternateDouble:
							playerNum = ShotNum % 4 < 2 ? 0 : 1;
							if ( WaveNum % 3 == 0 )
								playerNum = Globals.GetOpponentPlayerNum( playerNum );

							SpawnBall( playerNum, toLeft: true, ballSpeed );
							SpawnBall( Globals.GetOpponentPlayerNum( playerNum ), toLeft: false, ballSpeed );
							break;
						case DispenserPattern.AlternateTriple:
							playerNum = ShotNum % 6 < 3 ? 0 : 1;
							if ( WaveNum % 7 > 4 )
								playerNum = Globals.GetOpponentPlayerNum( playerNum );

							SpawnBall( playerNum, toLeft: true, ballSpeed );
							SpawnBall( Globals.GetOpponentPlayerNum( playerNum ), toLeft: false, ballSpeed );
							break;
						case DispenserPattern.FiveToOne:
							playerNum = ShotNum % 6 == (WaveNum % 6) ? 0 : 1;
							if ( WaveNum % 2 == 0 )
								playerNum = Globals.GetOpponentPlayerNum( playerNum );

							SpawnBall( playerNum, toLeft: true, ballSpeed );
							SpawnBall( Globals.GetOpponentPlayerNum( playerNum ), toLeft: false, ballSpeed );
							break;
					}

					PlayShootEffects();

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
			float delayTime = Utils.Map( Manager.Instance.TimeSincePhaseChange, 0f, Utils.Map( Manager.Instance.RoundNum, 1, 12, 90f, 30f ), 1f, 0.5f );
			if ( TimeSinceWaveEnded > delayTime && Manager.Instance.GamePhase == GamePhase.RoundActive )
			{
				StartWave();
			}
		}
	}

	void SpawnBall(int playerNum, bool toLeft, float speed)
	{
		var dir = toLeft ? new Vector2( -1f, 0f ) : new Vector2( 1f, 0f );
		Manager.Instance.SpawnBall( (Vector2)Transform.Position + dir, dir * speed, playerNum );
	}

	[Broadcast]
	public void PlayShootEffects()
	{
		Sound.Play( "shoot-enemy-default-light", Transform.LocalPosition.WithZ(Globals.SFX_HEIGHT) );
	}

	public void StartNewMatch()
	{
		WaveNum = 0;
	}

	public void StartWave()
	{
		IsGoingUp = !IsGoingUp;
		Transform.LocalPosition = IsGoingUp ? _botPos : _topPos;
		IsWaveActive = true;
		TimeSinceShoot = 0f;
		ShotNum = 0;
		_dispenserPattern = (DispenserPattern)Game.Random.Int(0, Enum.GetValues( typeof( DispenserPattern ) ).Length - 1 );

		Speed = 40f;
	}

	public void WaveFinished()
	{
		IsWaveActive = false;
		TimeSinceWaveEnded = 0f;
		WaveNum++;
	}
}
