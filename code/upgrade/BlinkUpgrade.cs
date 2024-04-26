using Sandbox;
using System;

public class BlinkUpgrade : Upgrade
{
	public override string SfxUse => "";

	public override void Use()
	{
		base.Use();

		Player.Transform.Position = new Vector3( Manager.Instance.MouseWorldPos.x, Manager.Instance.MouseWorldPos.y, Player.Transform.Position.z );
		Player.CheckBoundsPlaying();

		Manager.Instance.PlaySfx( "blink_target", Player.Transform.Position );
	}
}
