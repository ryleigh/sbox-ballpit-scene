using Sandbox;

public sealed class FadingText : Component
{
	[Property] public TextRenderer TextRenderer { get; set; }

	private TimeSince _timeSinceSpawn;

	public float Lifetime { get; set; }

	public void Init(string text, float lifetime)
	{
		TextRenderer.Text = text;
		Lifetime = lifetime;
	}

	protected override void OnStart()
	{
		base.OnStart();

		_timeSinceSpawn = 0f;
		TextRenderer.Color = Color.White.WithAlpha( 0f );
	}

	protected override void OnUpdate()
	{
		float opacity = Utils.Map( _timeSinceSpawn, 0f, 0.5f, 0f, 1f, EasingType.QuadOut ) * Utils.Map( _timeSinceSpawn, 0f, Lifetime - 0.1f, 1f, 0.1f, EasingType.ExpoIn ) * Utils.Map( _timeSinceSpawn, Lifetime - 0.5f, Lifetime - 0.1f, 1f, 0f, EasingType.Linear );
		TextRenderer.Color = (new Color(0.8f, 0.8f, 0.8f)).WithAlpha(opacity);
		TextRenderer.Scale = Utils.Map( _timeSinceSpawn, 0f, 1f, 1.2f, 1f, EasingType.SineOut ) * Utils.Map( _timeSinceSpawn, 0f, Lifetime - 0.1f, 0.3f, 0.35f, EasingType.SineIn );

		if ( _timeSinceSpawn > Lifetime )
			GameObject.Destroy();
	}
}
