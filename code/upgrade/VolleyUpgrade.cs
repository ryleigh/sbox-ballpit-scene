using Sandbox;
using System;

public class VolleyUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		var playerPos = Player.Transform.Position;

		var currDegrees = -30f;
		for ( int i = 0; i < 5; i++ )
		{
			var forwardDegrees = Utils.VectorToDegrees( Manager.MouseWorldPos - (Vector2)playerPos );
			var vec = Utils.DegreesToVector( currDegrees + forwardDegrees );
			var speed = 85f;
			Manager.SpawnBall( Player.Pos2D + vec * 25f, vec * speed, Player.PlayerNum, radius: 8f );
			currDegrees += 15f;
		}
	}
}
