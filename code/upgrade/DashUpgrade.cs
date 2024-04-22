using Sandbox;
using System;

public class DashUpgrade : Upgrade
{
	private bool _isDashing;
	private TimeSince _timeSinceDashStarted;
	private float _dashTime;
	public Vector2 DashVelocity { get; private set; }

	public override void Use()
	{
		base.Use();

		var vel = (Manager.Instance.MouseWorldPos - (Vector2)Player.Transform.Position).Normal * 400f;
		Dash( vel, 0.5f );
	}

	public override void Update( float dt )
	{
		base.Update( dt );

		if ( _isDashing )
		{
			if ( _timeSinceDashStarted > _dashTime )
			{
				_isDashing = false;
				DashVelocity = Vector2.Zero;
			}
			else
			{
				Player.Transform.Position += (Vector3)DashVelocity * Utils.Map( _timeSinceDashStarted, 0f, _dashTime, 1f, 0f, EasingType.SineOut ) * Time.Delta;
				Player.Transform.Position = Player.Transform.Position.WithZ( Player.IsSpectator ? Manager.SPECTATOR_HEIGHT : 0f );
			}
		}
	}

	public void Dash( Vector2 vel, float time )
	{
		_isDashing = true;
		_timeSinceDashStarted = 0f;
		_dashTime = time;
		DashVelocity = vel;
	}
}
