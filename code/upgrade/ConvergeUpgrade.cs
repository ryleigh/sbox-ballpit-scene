﻿using Sandbox;
using System;

public class ConvergeUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Manager.PlaySfx( "bubble", Player.Transform.Position );

		var enemy = Manager.GetPlayer( Globals.GetOpponentPlayerNum( Player.PlayerNum ) );
		var enemyPos = enemy?.Transform.Position ?? Vector3.Zero;

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( ball.IsActive && ball.PlayerNum == Player.PlayerNum )
			{
				var dir = ((Vector2)enemyPos - (Vector2)ball.Transform.Position).Normal;
				var speed = ball.Velocity.Length;
				ball.SetVelocity( dir * speed, timeScale: 0f, duration: 0.5f, EasingType.QuadIn );
			}
		}
	}
}
