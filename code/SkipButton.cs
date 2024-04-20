using Sandbox;

public class SkipButton : Component
{
	[Sync] public bool ShouldFlash { get; set; }

	public bool FlashToggle { get; set; }
	private TimeSince _timeSinceFlash;

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

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( ShouldFlash)
		{
			if(_timeSinceFlash > (FlashToggle ? 0.1f : 0.15f))
			{
				FlashToggle = !FlashToggle;
				_timeSinceFlash = 0f;
			}
		}

		var bobSpeed = BobSpeed * (ShouldFlash ? 2f : 1f);

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

	[Broadcast]
	public void StartFlashing()
	{
		ShouldFlash = true;
	}
}
