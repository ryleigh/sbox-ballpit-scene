using Sandbox;
using System;

public class CleaveUpgrade : Upgrade
{
	public static float GetChance( int level ) => Utils.Map( level, 0, 6, 0.2f, 0.9f );

	public const float RADIUS = 70f;
	public const float RECHARGE_DELAY = 0.4f;

	public TimeSince TimeSinceRedirect { get; private set; }

	public override void BumpOwnBall( Ball ball )
	{
		base.BumpOwnBall( ball );

		if ( Game.Random.Float( 0f, 1f ) > GetChance( Level ) || TimeSinceRedirect < RECHARGE_DELAY )
			return;

		var dir = ((Vector2)ball.Transform.Position - (Vector2)Player.Transform.Position).Normal;

		foreach ( var b in Scene.GetAllComponents<Ball>() )
		{
			if ( b.IsDespawning || b == ball || b.PlayerNum != Player.PlayerNum )
				continue;

			var distSqr = ((Vector2)b.Transform.Position - (Vector2)Player.Transform.Position).LengthSquared;
			if ( distSqr < MathF.Pow( RADIUS, 2f ) )
			{
				var speed = b.Velocity.Length;
				b.SetVelocity( dir * speed, timeScale: 0f, duration: 0.3f, EasingType.ExpoIn, showArrow: true );
			}
		}

		TimeSinceRedirect = 0f;
	}
}
