using Sandbox;

public enum MoneyMoveMode { SineWave, Tossed, }

public class MoneyPickup : Component
{
	[Sync] public int NumLevels { get; set; }

	public const float HEIGHT = 20f;

	public MoneyMoveMode MoneyMoveMode { get; set; }

	private float _frequency;
	private float _amplitude;
	private bool _startAtTop;

	private Vector2 _startPos;
	private Vector2 _endPos;
	private bool _isTossed;
	private float _tossTime;
	private TimeSince _timeSinceToss;

	[Broadcast]
	public void Init( int numLevels, bool startAtTop )
	{
		if ( IsProxy )
			return;

		MoneyMoveMode = MoneyMoveMode.SineWave;

		NumLevels = numLevels;

		_frequency = Game.Random.Float( 1.5f, 2.5f ) * Utils.Map( Manager.Instance.TimeSincePhaseChange, 0f, 120f, 0.95f, 1.4f );
		_amplitude = Game.Random.Float( 70f, 135f ) * (Game.Random.Int( 0, 1 ) == 0 ? 1f : -1f);
		_startAtTop = startAtTop;
	}

	[Broadcast]
	public void Init( int numLevels, Vector2 startPos, Vector2 endPos, float time )
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
				break;
			case MoneyMoveMode.Tossed:
				if(_isTossed)
				{
					if ( _timeSinceToss > _tossTime )
					{
						Transform.Position = new Vector3( _endPos.x, _endPos.y, HEIGHT );
						_isTossed = false;
					}
					else
					{
						float yOffset = Utils.MapReturn( _timeSinceToss, 0f, _tossTime, 0f, 80f, EasingType.SineInOut );
						Vector2 pos = Vector2.Lerp( _startPos, _endPos, Utils.Map( _timeSinceToss, 0f, _tossTime, 0f, 1f, EasingType.SineIn ) );
						Transform.Position = new Vector3( pos.x, pos.y + yOffset, HEIGHT );
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
