using Sandbox;
using System;

public class GatherUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Manager.PlaySfx( "bubble", Player.Transform.Position );

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( ball.IsActive && ball.PlayerNum == Player.PlayerNum )
			{
				var speed = ball.Velocity.Length;
				var dir = ((Vector2)Player.Transform.Position - (Vector2)ball.Transform.Position).Normal;
				ball.SetVelocity( dir * speed, timeScale: 0f, duration: 0.2f, EasingType.Linear );
			}
		}
	}
}
