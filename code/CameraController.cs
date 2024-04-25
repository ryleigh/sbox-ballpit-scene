using Sandbox;

public class CameraController : Component
{
	public Vector3 _startingPos;
	private bool _isShaking;
	private RealTimeSince _timeSinceShakeStart;
	private float _shakeDuration;
	private float _shakeStrength;

	protected override void OnStart()
	{
		base.OnStart();

		_startingPos = Transform.Position;
	}

	protected override void OnUpdate()
	{
		if ( !_isShaking )
			return;

		if(_timeSinceShakeStart > _shakeDuration)
		{
			_isShaking = false;
			Transform.Position = new Vector3(0f, 0f, _startingPos.z);
		}
		else
		{
			var amount = Utils.Map( _timeSinceShakeStart, 0f, _shakeDuration, _shakeStrength, 0f, EasingType.QuadOut );
			var vec = Utils.GetRandomVector();
			Transform.Position = new Vector3( vec.x * amount, vec.y * amount, _startingPos.z );
		}
	}

	[Broadcast]
	public void Shake(float strength, float duration)
	{
		if ( _isShaking && (_shakeDuration - _timeSinceShakeStart > 0.1f) && _shakeStrength > strength )
			return;

		_isShaking = true;
		_shakeStrength = strength;
		_shakeDuration = duration;
		_timeSinceShakeStart = 0f;
	}
}
