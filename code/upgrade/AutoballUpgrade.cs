using Sandbox;
using System;

public class AutoballUpgrade : Upgrade
{
	private float _autoballTimer;

	public override void Update( float dt )
	{
		base.Update( dt );

		_autoballTimer += Time.Delta;
		float reqTime = Utils.Map( Level, 1, Manager.GetMaxLevelForUpgrade( UpgradeType.Autoball ), 5f, 1f );
		if ( _autoballTimer > reqTime )
		{
			var speed = 85f;
			var dir = Utils.GetRandomVector();
			Manager.SpawnBall( Player.Pos2D + dir * 15f, dir * speed, Player.PlayerNum, radius: 8f );

			_autoballTimer = 0f;
		}
	}
}
