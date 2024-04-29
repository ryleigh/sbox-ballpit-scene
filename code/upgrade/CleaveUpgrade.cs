using Sandbox;
using System;

public class CleaveUpgrade : Upgrade
{
	public static float GetChance( int level ) => Utils.Map( level, 0, 6, 0.2f, 0.9f );

	public const float RADIUS = 50f;
	public const float RECHARGE_DELAY = 0.4f;

	public TimeSince TimeSinceRedirect { get; private set; }

	public override void BumpOwnBall( Ball ball )
	{
		base.BumpOwnBall( ball );

		if ( Game.Random.Float( 0f, 1f ) > GetChance( Level ) || TimeSinceRedirect < RECHARGE_DELAY )
			return;

		var playerPos = (Vector2)Player.Transform.Position;

		var dir = ((Vector2)ball.Transform.Position - playerPos).Normal;

		foreach ( var b in Scene.GetAllComponents<Ball>() )
		{
			if ( b.IsDespawning || b == ball || b.PlayerNum != Player.PlayerNum )
				continue;

			var distSqr = ((Vector2)b.Transform.Position - playerPos).LengthSquared;
			if ( distSqr < MathF.Pow( RADIUS, 2f ) )
			{
				var speed = b.Velocity.Length;
				b.SetVelocity( dir * speed, timeScale: 0f, duration: 0.3f, EasingType.ExpoIn, showArrow: true );
			}
		}

		TimeSinceRedirect = 0f;

		Manager.Instance.SpawnRingVfx( playerPos, Game.Random.Float(0.2f, 0.25f), Color.White, Color.White.WithAlpha(0f), RADIUS * 0.33f, RADIUS * 0.9f, outlineWidthStart: 1.5f, outlineWidthEnd: 1f, EasingType.ExpoOut );

		Manager.Instance.PlaySfx( "bubble", Player.Transform.Position, volume: 0.9f, pitch: Game.Random.Float( 1.2f, 1.3f ) );
	}
}
