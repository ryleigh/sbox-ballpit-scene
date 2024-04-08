using Sandbox;
using System;

public class AutoballUpgrade : Upgrade
{
	public static float GetDelay( int level ) => Utils.Map( level, 1, 9, 5f, 1f );

	private float _autoballTimer;

	public override void Update( float dt )
	{
		base.Update( dt );

		if ( Manager.GamePhase != GamePhase.RoundActive )
			return;

		_autoballTimer += Time.Delta;
		var delay = GetDelay( Level );
		if ( _autoballTimer > delay )
		{
			var speed = 85f;
			var dir = Utils.GetRandomVector();
			Manager.SpawnBall( Player.Pos2D + dir * 15f, dir * speed, Player.PlayerNum, radius: 8f );

			_autoballTimer = 0f;

			Player.PassiveUpgradeProgress[UpgradeType.Autoball] = 0f;
		}
		else
		{
			Player.PassiveUpgradeProgress[UpgradeType.Autoball] = Math.Clamp( _autoballTimer / delay, 0f, 1f );
		}
	}
}
