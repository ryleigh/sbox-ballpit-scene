using Sandbox;
using System;

public class BlackHoleUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Manager.PlaySfx( "bubble", Vector2.Zero );

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( !ball.IsDespawning )
			{
				var dir = (Vector2.Zero - (Vector2)ball.Transform.Position).Normal;
				var speed = ball.Velocity.Length * 1.25f;
				ball.SetVelocity( dir * speed, timeScale: 0f, duration: 0.6f, EasingType.ExpoIn );
			}
		}
	}
}
