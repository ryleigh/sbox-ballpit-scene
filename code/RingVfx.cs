using Sandbox;

public class RingVfx : Component
{
	[Property] public HighlightOutline Outline { get; set; }

	public TimeSince TimeSinceSpawn { get; private set; }

	private float _lifetime;
	private Color _colorStart;
	private Color _colorEnd;
	private float _radiusStart;
	private float _radiusEnd;
	private float _outlineWidthStart;
	private float _outlineWidthEnd;
	private Color _colorInsideStart;
	private Color _colorInsideEnd;
	private EasingType _easingType;

	public void Init(float lifetime, Color colorStart, Color colorEnd, float radiusStart, float radiusEnd, float outlineWidthStart, float outlineWidthEnd, Color colorInsideStart, Color colorInsideEnd, EasingType easingType )
	{
		TimeSinceSpawn = 0f;

		_lifetime = lifetime;
		_colorStart = colorStart;
		_colorEnd = colorEnd;
		_radiusStart = radiusStart;
		_radiusEnd = radiusEnd;
		_outlineWidthStart = outlineWidthStart;
		_outlineWidthEnd = outlineWidthEnd;
		_colorInsideStart = colorInsideStart;
		_colorInsideEnd = colorInsideEnd;
		_easingType = easingType;

		Outline.ObscuredColor = Color.Transparent;
		Outline.InsideObscuredColor = Color.Transparent;
	}

	protected override void OnUpdate()
	{
		if( TimeSinceSpawn > _lifetime ) 
		{
			GameObject.Destroy();
		}
		else
		{
			var progress = Utils.Map( TimeSinceSpawn, 0f, _lifetime, 0f, 1f, _easingType );

			var opacityMult = Utils.Map( TimeSinceSpawn, 0f, 0.1f, 0f, 1f, EasingType.Linear ) * Utils.Map( TimeSinceSpawn, _lifetime - 0.1f, _lifetime, 1f, 0f, EasingType.Linear );

			var color = Color.Lerp( _colorStart.WithAlpha( _colorStart.a * opacityMult ), _colorEnd.WithAlpha( _colorEnd.a * opacityMult ), progress ); ;
			Outline.Color = color;
			//Outline.ObscuredColor = color;

			var colorInside = Color.Lerp( _colorInsideStart.WithAlpha( _colorInsideStart.a * opacityMult ), _colorInsideEnd.WithAlpha( _colorInsideEnd.a * opacityMult ), progress ); ;
			Outline.InsideColor = colorInside;
			//Outline.InsideObscuredColor = colorInside;

			Outline.Width = Utils.Map( progress, 0f, 1f, _outlineWidthStart, _outlineWidthEnd );

			var scale = Utils.Map( progress, 0f, 1f, _radiusStart, _radiusEnd ) / 30f;
			GameObject.Transform.Scale = new Vector3( scale, scale, 0.1f );
		}
	}
}
