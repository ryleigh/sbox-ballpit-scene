using Sandbox;
using System;

public class RepelUpgrade : Upgrade
{
	public override string SfxUse => "repel_use";

	public override void Use()
	{
		base.Use();

		Manager.Instance.SpawnRepelEffect((Vector2)Player.Transform.Position);

		Manager.Instance.CameraController.Shake( 1.4f, Game.Random.Float( 0.1f, 0.15f ) );

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( ball.IsDespawning )
				continue;

			var distSqr = ((Vector2)ball.Transform.Position - (Vector2)Player.Transform.Position).LengthSquared;
			if ( distSqr < MathF.Pow( 100f, 2f ) )
			{
				var speed = ball.Velocity.Length * 1.15f;
				var dir = ((Vector2)ball.Transform.Position - (Vector2)Player.Transform.Position).Normal;
				ball.SetVelocity( dir * speed, timeScale: 0f, duration: 0.1f, EasingType.ExpoIn, showArrow: false );
			}
		}
	}
}
