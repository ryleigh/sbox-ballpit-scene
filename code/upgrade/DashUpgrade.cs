using Sandbox;
using System;

public class DashUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Manager.PlaySfx( "bubble", Player.Transform.Position );

		var vel = (Manager.Instance.MouseWorldPos - (Vector2)Player.Transform.Position).Normal * 400f;
		Player.Dash( vel, 0.5f );
	}
}
