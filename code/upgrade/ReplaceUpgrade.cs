using Sandbox;
using System;

public class ReplaceUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Manager.PlaySfx( "bubble", Player.Transform.Position );

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( !ball.IsActive )
				continue;

			ball.SetTimeScaleRPC( timeScale: 0f, duration: 0.75f, EasingType.QuadIn );
			ball.SetPlayerNum( Globals.GetOpponentPlayerNum( ball.PlayerNum ) );
		}
	}
}
