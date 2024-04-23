using Sandbox;
using System;

public class ReplaceUpgrade : Upgrade
{
	public override string SfxUse => "replace_use";

	public override void Use()
	{
		base.Use();

		foreach ( var ball in Scene.GetAllComponents<Ball>() )
		{
			if ( ball.IsDespawning )
				continue;

			ball.SetTimeScaleRPC( timeScale: 0f, duration: 0.75f, EasingType.QuadIn );
			ball.SetPlayerNum( Globals.GetOpponentPlayerNum( ball.PlayerNum ) );
		}
	}
}
