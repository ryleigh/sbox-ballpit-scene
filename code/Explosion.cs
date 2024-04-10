using Sandbox;

public class Explosion : Component
{
	[Property] public ModelRenderer Renderer { get; set; }

	private TimeSince _timeSinceColorChange;
	private bool _colorToggled;

	private Color _colorA;
	private Color _colorB;

	public TimeSince TimeSinceSpawn { get; set; }
	public float Lifetime = 1.5f;

	public float Scale { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		_colorA = new Color( 0.8f, 0f, 0f );
		_colorB = new Color( 1f, 1f, 0f );
		TimeSinceSpawn = 0f;
	}

	protected override void OnUpdate()
	{
		if ( _timeSinceColorChange > 0.05f )
		{
			Renderer.Tint = (_colorToggled ? _colorA : _colorB).WithAlpha(Utils.Map( TimeSinceSpawn, 0f, Lifetime, 1f, 0f, EasingType.Linear));
			_colorToggled = !_colorToggled;
			_timeSinceColorChange = 0f;
		}

		Transform.Scale = Utils.Map( TimeSinceSpawn, 0f, Lifetime, Scale, Scale * 0.7f, EasingType.SineOut ) * Utils.MapReturn( TimeSinceSpawn, 0f, 0.1f, 1.2f, 1f);

		if ( IsProxy )
			return;

		if ( TimeSinceSpawn > Lifetime )
			GameObject.Destroy();
	}
}
