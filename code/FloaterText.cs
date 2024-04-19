using Sandbox;
using System.Drawing;

public sealed class FloaterText : Component
{
	private TimeSince _timeSinceSpawn;

	public float Lifetime { get; set; }

	private float _startScale;
	private float _endScale;
	private Vector2 _velocity;
	private float _deceleration;

	public string Text { get; set; }
	public Color TextColor { get; set; }
	public float FontSize { get; set; }
	public float Scale { get; set; }
	public float Opacity { get; set; }
	public bool IsEmoji { get; set; }

	public void Init( string text, float lifetime, Color color, Vector2 velocity, float deceleration, float fontSize, float startScale, float endScale, bool isEmoji )
	{
		Text = text;
		Lifetime = lifetime;
		TextColor = color;
		_velocity = velocity;
		_deceleration = deceleration;
		FontSize = fontSize;
		Scale = _startScale = startScale;
		_endScale = endScale;
		IsEmoji = isEmoji;
	}

	protected override void OnStart()
	{
		base.OnStart();

		_timeSinceSpawn = 0f;
	}

	protected override void OnUpdate()
	{
		Opacity = Utils.Map( _timeSinceSpawn, 0f, 0.3f, 0f, 1f, EasingType.QuadOut ) * Utils.Map( _timeSinceSpawn, 0f, Lifetime - 0.1f, 1f, 0.1f, EasingType.ExpoIn ) * Utils.Map( _timeSinceSpawn, Lifetime - 0.3f, Lifetime - 0.05f, 1f, 0f, EasingType.Linear );
		Scale = Utils.Map( _timeSinceSpawn, 0f, Lifetime, _startScale, _endScale, EasingType.SineOut );

		Transform.Position += new Vector3( _velocity.x, _velocity.y, 0f ) * Time.Delta;
		_velocity *= (1f -  _deceleration * Time.Delta);

		if ( _timeSinceSpawn > Lifetime )
			GameObject.Destroy();
	}
}
