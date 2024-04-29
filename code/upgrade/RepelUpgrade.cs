using Sandbox;
using System;

public class RepelUpgrade : Upgrade
{
	public override string SfxUse => "repel_use";

	public const float RADIUS = 85f;

	public override void Use()
	{
		base.Use();

		var playerPos = (Vector2)Player.Transform.Position;

		var colorStart = new Color( 0.4f, 0.1f, 0.9f );
		var colorEnd = new Color( 0.4f, 0.15f, 1f );

		Manager.Instance.SpawnRingVfxRPC( playerPos, Game.Random.Float( 0.23f, 0.26f ), colorStart, colorEnd.WithAlpha( 0.15f ), RADIUS * 0.33f, RADIUS * 0.8f, outlineWidthStart: 1.5f, outlineWidthEnd: 2.5f,
			colorInsideStart: colorStart, colorInsideEnd: colorEnd.WithAlpha( 0f ), EasingType.QuadOut );

		Manager.Instance.CameraController.Shake( 1.4f, Game.Random.Float( 0.1f, 0.15f ) );

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( ball.IsDespawning )
				continue;

			var distSqr = ((Vector2)ball.Transform.Position - playerPos).LengthSquared;
			if ( distSqr < MathF.Pow( RADIUS, 2f ) )
			{
				var speed = ball.Velocity.Length * 1.15f;
				var dir = ((Vector2)ball.Transform.Position - playerPos).Normal;
				ball.SetVelocity( dir * speed, timeScale: 0f, duration: 0.25f, EasingType.ExpoIn, showArrow: true );
			}
		}
	}
}
