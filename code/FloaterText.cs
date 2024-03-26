using Sandbox;
using System.Drawing;

public sealed class FloaterText : Component
{
	[Property] public TextRenderer TextRenderer { get; set; }

	private TimeSince _timeSinceSpawn;

	public float Lifetime { get; set; }

	private Color _color;
	private float _startScale;
	private float _endScale;
	private Vector2 _velocity;
	private float _deceleration;

	public void Init( string text, float lifetime, Color color, Vector2 velocity, float deceleration, float startScale, float endScale )
	{
		TextRenderer.Text = text;
		TextRenderer.Scale = startScale;
		Lifetime = lifetime;
		_color = color;
		_velocity = velocity;
		_deceleration = deceleration;
		_startScale = startScale;
		_endScale = endScale;
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
		TextRenderer.Color = _color.WithAlpha( opacity );
		TextRenderer.Scale = Utils.Map( _timeSinceSpawn, 0f, Lifetime, _startScale, _endScale, EasingType.SineOut );

		Transform.Position += new Vector3( _velocity.x, _velocity.y, 0f ) * Time.Delta;
		_velocity *= (1f -  _deceleration * Time.Delta);

		if ( _timeSinceSpawn > Lifetime )
			GameObject.Destroy();
	}
}
