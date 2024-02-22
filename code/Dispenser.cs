using Sandbox;

public sealed class Dispenser : Component
{
	public const float HEIGHT = 40f;
	private Vector3 _topPos;
	private Vector3 _botPos;

	public float Speed { get; private set; }
	public bool IsGoingUp { get; private set; }

	[Sync] public int WaveNum { get; private set; }

	public bool IsWaveActive { get; private set; }
	public TimeSince TimeSinceWaveEnded { get; private set; }

	protected override void OnStart()
	{
		base.OnAwake();

		if ( IsProxy )
			return;

		_topPos = new Vector3( 0f, 160f, HEIGHT );
		_botPos = new Vector3( 0f, -160f, HEIGHT );

		Speed = 40f;

		StartWave();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if( IsWaveActive )
		{
			Transform.Position = Transform.Position.WithY( Transform.Position.y + Speed * (IsGoingUp ? 1f : -1f) * Time.Delta );

			if((IsGoingUp && Transform.Position.y > _topPos.y) || (!IsGoingUp && Transform.Position.y < _botPos.y))
			{
				WaveFinished();
			}
		}
		else
		{
			if( TimeSinceWaveEnded > 1f)
			{
				IsGoingUp = !IsGoingUp;
				StartWave();
			}
		}
	}

	public void StartWave()
	{
		Transform.Position = IsGoingUp ? _botPos : _topPos;
		IsWaveActive = true;
	}

	public void WaveFinished()
	{
		IsWaveActive = false;
		TimeSinceWaveEnded = 0f;
	}
}
