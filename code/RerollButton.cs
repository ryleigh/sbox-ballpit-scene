using Sandbox;

public class RerollButton : Component
{
	[Sync] public int PlayerNum { get; set; }

	public float BobSpeed { get; private set; }
	public float ShadowDistance { get; private set; }
	public float ShadowBlur { get; private set; }
	public float ShadowOpacity { get; private set; }
	public Vector2 IconOffset { get; private set; }
	private float _timingOffset;

	protected override void OnAwake()
	{
		base.OnAwake();

		BobSpeed = Game.Random.Float( 3f, 3.5f );
		_timingOffset = Game.Random.Float( 0f, 5f );
	}

	[Broadcast]
	public void Init( int playerNum )
	{
		if ( IsProxy )
			return;

		PlayerNum = playerNum;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		var player = Manager.Instance.GetPlayer( PlayerNum );
		if ( player == null )
			return;

		var cannotAfford = player.Money < player.CurrRerollPrice;

		var bobSpeed = BobSpeed * (cannotAfford ? 0.5f : 1f);

		IconOffset = new Vector2( 0f, Utils.FastSin( _timingOffset + Time.Now * bobSpeed ) * 3.5f );
		ShadowDistance = 9f + Utils.FastSin( _timingOffset + Time.Now * bobSpeed ) * 3f;
		ShadowBlur = 2.3f + Utils.FastSin( _timingOffset + Time.Now * bobSpeed ) * 1.2f;
		ShadowOpacity = 0.85f - Utils.FastSin( _timingOffset + Time.Now * bobSpeed ) * 0.15f;
	}

	[Broadcast]
	public void DestroyButton()
	{
		if ( IsProxy )
			return;

		GameObject.Destroy();
	}
}
