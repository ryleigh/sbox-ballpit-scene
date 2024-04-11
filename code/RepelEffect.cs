using Sandbox;

public class RepelEffect : Component
{
	[Property] public ModelRenderer Renderer { get; set; }

	public TimeSince TimeSinceSpawn { get; set; }
	public float Lifetime = 0.25f;

	private Color _color;

	protected override void OnStart()
	{
		base.OnStart();

		TimeSinceSpawn = 0f;
		_color = new Color( 0.4f, 0f, 1f );
	}

	protected override void OnUpdate()
	{
		Renderer.Tint = _color.WithAlpha( Utils.Map( TimeSinceSpawn, 0f, Lifetime, 0.85f, 0f, EasingType.SineIn ) );

		Transform.Scale = Utils.Map( TimeSinceSpawn, 0f, Lifetime, 0f, 2.4f, EasingType.SineOut ) * Utils.MapReturn( TimeSinceSpawn, 0f, 0.1f, 1.2f, 0.8f, EasingType.QuadOut );

		if ( TimeSinceSpawn > Lifetime )
			GameObject.Destroy();
	}
}
