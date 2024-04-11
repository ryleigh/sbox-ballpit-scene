using Sandbox;
using System;

public class SlowmoUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Manager.PlaySfx( "bubble", Player.Transform.Position );
		Manager.SlowmoRPC( 0.2f, 2.5f, EasingType.ExpoIn );
	}
}
