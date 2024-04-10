using Sandbox;

public class Explosion : Component
{
	[Property] public ModelRenderer Renderer { get; set; }

	private Color _colorA;
	private Color _colorB;

	public TimeSince TimeSinceSpawn { get; set; }
	public float Lifetime = 1f;

	public float Scale { get; set; }

	public bool DealtDamage { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		_colorA = new Color( 0.8f, 0f, 0f );
		_colorB = new Color( 1f, 1f, 0f );
		TimeSinceSpawn = 0f;
	}

	protected override void OnUpdate()
	{
		Renderer.Tint = Color.Lerp( _colorA, _colorB, Utils.FastSin(TimeSinceSpawn * 32f) ).WithAlpha( Utils.Map( TimeSinceSpawn, 0f, Lifetime, 1f, 0f, EasingType.QuadOut ) );

		Transform.Scale = Utils.Map( TimeSinceSpawn, 0f, Lifetime, Scale, Scale * 1.25f, EasingType.QuadIn ) * Utils.MapReturn( TimeSinceSpawn, 0f, 0.1f, 1.3f, 0.7f, EasingType.Linear);

		if ( IsProxy )
			return;

		if ( TimeSinceSpawn > Lifetime )
			GameObject.Destroy();
	}
}
