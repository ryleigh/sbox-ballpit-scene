using Sandbox;

public class PickupItem : Component
{
	[Sync] public UpgradeType UpgradeType { get; set; }
	[Sync] public int NumLevels { get; set; }

	public const float HEIGHT = 20f;

	private float _frequency;
	private float _amplitude;
	private bool _startAtTop;

	//protected override void OnUpdate()
	//{
	//	Gizmo.Draw.Color = Color.White;
	//	Gizmo.Draw.Text( $"{NumLevels}\n${Price}", new global::Transform( Transform.Position + new Vector3( 0f, 20f, 1f ) ) );
	//}

	[Broadcast]
	public void Init( UpgradeType upgradeType, int numLevels, bool startAtTop )
	{
		if ( IsProxy )
			return;

		UpgradeType = upgradeType;
		NumLevels = numLevels;

		_frequency = Game.Random.Float( 1.5f, 2.5f ) * Utils.Map(Manager.Instance.TimeSincePhaseChange, 0f, 120f, 0.95f, 1.4f);
		_amplitude = Game.Random.Float( 70f, 135f ) * ( Game.Random.Int( 0, 1 ) == 0 ? 1f : -1f );
		_startAtTop = startAtTop;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsProxy )
			return;

		Transform.Position = new Vector3( Utils.FastSin( Time.Now * _frequency ) * _amplitude, Transform.Position.y - 25f * ( _startAtTop ? 1f : -1f ) * Time.Delta, HEIGHT );

		if ( (_startAtTop && Transform.Position.y < -120f) || (!_startAtTop && Transform.Position.y > 120f) )
			GameObject.Destroy();
	}

	[Broadcast]
	public void DestroyRPC()
	{
		if ( IsProxy )
			return;

		GameObject.Destroy();
	}
}
