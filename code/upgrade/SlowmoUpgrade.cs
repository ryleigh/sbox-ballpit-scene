using Sandbox;
using System;

public class SlowmoUpgrade : Upgrade
{
	public override string SfxUse => "slowmo_use";

	public override void Use()
	{
		base.Use();

		Manager.SlowmoRPC( 0.2f, 2.5f, EasingType.ExpoIn );
	}
}
