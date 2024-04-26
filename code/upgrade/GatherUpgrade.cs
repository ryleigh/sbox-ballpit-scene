using Sandbox;
using System;

public class GatherUpgrade : Upgrade
{
	public override string SfxUse => "gather_use";

	public override void Use()
	{
		base.Use();

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( !ball.IsDespawning && ball.PlayerNum == Player.PlayerNum )
			{
				var speed = ball.Velocity.Length;
				var dir = ((Vector2)Player.Transform.Position - (Vector2)ball.Transform.Position).Normal;
				ball.SetVelocity( dir * speed, timeScale: 0f, duration: 0.2f, EasingType.Linear );
			}
		}
	}
}
