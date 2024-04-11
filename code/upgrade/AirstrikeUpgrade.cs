using Sandbox;
using System;

public class AirstrikeUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		//Manager.Instance.SpawnExplosion( Manager.Instance.MouseWorldPos, 0.7f );
		Manager.Instance.SpawnFallingShadow( Manager.Instance.MouseWorldPos, 0.5f );

		Manager.Instance.PlaySfx( "bubble", Player.Transform.Position );
	}
}
