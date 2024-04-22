using Sandbox;
using System;

public class RedirectUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		var redirectDir = (Manager.Instance.MouseWorldPos - (Vector2)Player.Transform.Position).Normal;

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( !ball.IsDespawning && ball.PlayerNum == Player.PlayerNum )
			{
				var speed = ball.Velocity.Length;
				ball.SetVelocity( redirectDir * speed, timeScale: 0f, duration: 0.5f, EasingType.QuadIn );
			}
		}
	}
}
