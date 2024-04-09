using Sandbox;
using System.Numerics;

public enum MoneyMoveMode { SineWave, Tossed, Endow }

public class MoneyPickup : Component
{
	[Sync] public int NumLevels { get; set; }

	public const float HEIGHT = 20f;

	public MoneyMoveMode MoneyMoveMode { get; set; }

	// SineWave
	private float _frequency;
	private float _amplitude;
	private bool _startAtTop;

	// Tossed
	private Vector2 _startPos;
	private Vector2 _endPos;
	private bool _isTossed;
	private float _tossTime;
	private TimeSince _timeSinceToss;

	// Endow
	private int _startingSide;
	private Vector2 _dir;

	[Sync] public bool CanBePickedUp { get; private set; }
	[Sync] public bool IsFlying { get; private set; }


	[Broadcast]
	public void InitSineWave( int numLevels, bool startAtTop )
	{
		if ( IsProxy )
			return;

		MoneyMoveMode = MoneyMoveMode.SineWave;

		NumLevels = numLevels;

		_frequency = Game.Random.Float( 1.5f, 2.5f ) * Utils.Map( Manager.Instance.TimeSincePhaseChange, 0f, 120f, 0.95f, 1.4f );
		_amplitude = Game.Random.Float( 70f, 135f ) * (Game.Random.Int( 0, 1 ) == 0 ? 1f : -1f);
		_startAtTop = startAtTop;

		CanBePickedUp = true;
		IsFlying = true;
	}

	[Broadcast]
	public void InitTossed( int numLevels, Vector2 startPos, Vector2 endPos, float time )
	{
		if ( IsProxy )
			return;

		MoneyMoveMode = MoneyMoveMode.Tossed;

		NumLevels = numLevels;

		_startPos = startPos;
		_endPos = endPos;
		_isTossed = true;
		_tossTime = time;
		_timeSinceToss = 0f;

		CanBePickedUp = false;
		IsFlying = true;
	}

	[Broadcast]
	public void InitEndow( int numLevels, Vector2 startPos )
	{
		if ( IsProxy )
			return;

		MoneyMoveMode = MoneyMoveMode.Endow;

		NumLevels = numLevels;

		CanBePickedUp = false;
		IsFlying = true;

		_startingSide = startPos.x < 0f ? 0 : 1;
		_dir = _startingSide == 0 ? new Vector2( 1f, 0f ) : new Vector2( -1f, 0f );
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsProxy )
			return;

		switch(MoneyMoveMode)
		{
			case MoneyMoveMode.SineWave:
				Transform.Position = new Vector3( Utils.FastSin( Time.Now * _frequency ) * _amplitude, Transform.Position.y - 25f * (_startAtTop ? 1f : -1f) * Time.Delta, HEIGHT );

				if ( (_startAtTop && Transform.Position.y < -120f) || (!_startAtTop && Transform.Position.y > 120f) )
					GameObject.Destroy();

				break;
			case MoneyMoveMode.Tossed:
				if(_isTossed)
				{
					if ( _timeSinceToss > _tossTime )
					{
						Transform.Position = new Vector3( _endPos.x, _endPos.y, HEIGHT );
						_isTossed = false;
						CanBePickedUp = true;
						IsFlying = false;
					}
					else
					{
						float yOffset = Utils.MapReturn( _timeSinceToss, 0f, _tossTime, 0f, 80f, EasingType.SineInOut );
						Vector2 pos = Vector2.Lerp( _startPos, _endPos, Utils.Map( _timeSinceToss, 0f, _tossTime, 0f, 1f, EasingType.SineIn ) );
						Transform.Position = new Vector3( pos.x, pos.y + yOffset, HEIGHT );
					}
				}
				break;
			case MoneyMoveMode.Endow:
				Transform.Position += (Vector3)_dir * 160f * Time.Delta;

				if( !CanBePickedUp )
				{
					if ( ( _startingSide == 0 && Transform.Position.x > 0f) || (_startingSide == 1 && Transform.Position.x < 0f) )
						CanBePickedUp = true;
				}

				if( ( _startingSide == 0 && Transform.Position.x > Manager.X_FAR ) || (_startingSide == 1 && Transform.Position.x < -Manager.X_FAR) )
				{
					_dir = _dir.WithX( _dir.x * -1f );
					Manager.Instance.PlaySfx( "bubble", Transform.Position, volume: 0.3f, pitch: Game.Random.Float( 1.2f, 1.3f ) );
				}
					
				if ( (_startingSide == 0 && Transform.Position.x < -Manager.X_FAR) || (_startingSide == 1 && Transform.Position.x > Manager.X_FAR) )
					GameObject.Destroy();

				break;
		}
	}

	[Broadcast]
	public void DestroyRPC()
	{
		if ( IsProxy )
			return;

		GameObject.Destroy();
	}
}
