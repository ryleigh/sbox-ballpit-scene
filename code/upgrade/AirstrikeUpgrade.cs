using Sandbox;
using System;

public class AirstrikeUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		var connection = Manager.Instance.GetConnection( Manager.Instance.MouseWorldPos.x < 0f ? 0 : 1 );
		Manager.Instance.SpawnExplosion( connection, (Vector2)Manager.Instance.MouseWorldPos, 1f );

		Manager.Instance.PlaySfx( "bubble", Player.Transform.Position );
	}
}
