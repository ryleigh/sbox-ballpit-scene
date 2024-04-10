using Sandbox;

public class GutterBarrier : Component
{
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public int PlayerNum { get; set; }

	private TimeSince _timeSinceColorChange;
	private bool _colorToggled;

	private Color _colorA;
	private Color _colorB;

	private float _xScale;

	protected override void OnStart()
	{
		base.OnStart();

		_colorA = PlayerNum == 0 ? new Color( 0.1f, 0.1f, 1f ) : new Color( 0f, 0.3f, 0f );
		_colorB = PlayerNum == 0 ? new Color( 0.4f, 0.4f, 1f ) : new Color( 0.1f, 1f, 0.1f );

		_xScale = Transform.Scale.x;
	}

	protected override void OnUpdate()
	{
		if(_timeSinceColorChange > 0.1f)
		{
			Renderer.Tint = _colorToggled ? _colorA : _colorB;
			_colorToggled = !_colorToggled;
			_timeSinceColorChange = 0f;
		}

		var timeSince = PlayerNum == 0 ? Manager.Instance.TimeSinceLeftGutterBarrierRebound : Manager.Instance.TimeSinceRightGutterBarrierRebound;
		Transform.Scale = Transform.Scale.WithX( Utils.Map( timeSince, 0f, 0.33f, _xScale * 2.25f, _xScale, EasingType.BounceOut ) );
	}
}
