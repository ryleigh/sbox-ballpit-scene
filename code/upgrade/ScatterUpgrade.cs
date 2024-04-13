using Sandbox;
using System;

public class ScatterUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Manager.PlaySfx( "bubble", Player.Transform.Position );

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( ball.IsDespawning )
				continue;

			var speed = ball.Velocity.Length;
			var dir = Utils.GetRandomVector();
			ball.SetVelocity( dir * speed, timeScale: 0f, duration: 0.66f, EasingType.QuadIn );
		}
	}
}
