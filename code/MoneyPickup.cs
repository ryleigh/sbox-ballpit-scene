using Sandbox;
using System.Numerics;

public enum MoneyMoveMode { SineWave, Tossed, Endow }

public class MoneyPickup : Component
{
	[Sync] public int NumLevels { get; set; }
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

	public float Opacity { get; private set;  }

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
		Opacity = 0f;
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
		Opacity = 0f;
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
		Opacity = 0f;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		switch ( MoneyMoveMode )
		{
			case MoneyMoveMode.SineWave:
				Opacity = Utils.MapReturn( Transform.Position.y, -120f, 120f, 0f, 1f, EasingType.ExpoOut );
				break;
			case MoneyMoveMode.Tossed:
				Opacity = Transform.Position.y > 100f ? Utils.Map( Transform.Position.y, 100f, 130f, 1f, 0f, EasingType.QuadOut ) : 1f;
				break;
			case MoneyMoveMode.Endow:
				Opacity = Utils.Map( _timeSinceToss, 0f, 0.5f, 0f, 1f, EasingType.QuadOut ) * (CanBePickedUp ? 1f : 0.2f);
				break;
		}

		if ( IsProxy )
			return;

		switch(MoneyMoveMode)
		{
			case MoneyMoveMode.SineWave:
				Transform.Position = new Vector3( Utils.FastSin( Time.Now * _frequency ) * _amplitude, Transform.Position.y - 25f * (_startAtTop ? 1f : -1f) * Time.Delta, 0f );

				if ( (_startAtTop && Transform.Position.y < -120f) || (!_startAtTop && Transform.Position.y > 120f) )
					GameObject.Destroy();
				break;
			case MoneyMoveMode.Tossed:
				if(_isTossed)
				{
					if ( _timeSinceToss > _tossTime )
					{
						Transform.Position = new Vector3( _endPos.x, _endPos.y, 0f );
						_isTossed = false;
						CanBePickedUp = true;
						IsFlying = false;
					}
					else
					{
						Transform.Position = new Vector3( Utils.Map( _timeSinceToss, 0f, _tossTime, _startPos.x, _endPos.x, EasingType.Linear ), Utils.Map( _timeSinceToss, 0f, _tossTime, _startPos.y, _endPos.y, EasingType.QuadIn ), 0f );
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
					
				if ( _startingSide == 0 && Transform.Position.x < -Manager.X_FAR - 20f )
				{
					var leftBarrierActive = Manager.Instance.GetPlayer( 0 )?.IsBarrierActive ?? false;
					if ( leftBarrierActive )
					{
						_dir = _dir.WithX( _dir.x * -1f );
						Manager.Instance.PlaySfx( "bubble", Transform.Position, volume: 0.3f, pitch: Game.Random.Float( 1.2f, 1.3f ) );
					}
					else 
					{
						GameObject.Destroy();
					}
				}

				if ( _startingSide == 1 && Transform.Position.x > Manager.X_FAR + 20f )
				{
					var rightBarrierActive = Manager.Instance.GetPlayer( 1 )?.IsBarrierActive ?? false;
					if ( rightBarrierActive )
					{
						_dir = _dir.WithX( _dir.x * -1f );
						Manager.Instance.PlaySfx( "bubble", Transform.Position, volume: 0.3f, pitch: Game.Random.Float( 1.2f, 1.3f ) );
					}
					else
					{
						GameObject.Destroy();
					}
				}

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
