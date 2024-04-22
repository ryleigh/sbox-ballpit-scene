using Sandbox;
using System;
using System.Threading.Tasks;

public class FadeUpgrade : Upgrade
{
	public override string SfxUse => "fade_use";

	public override void Use()
	{
		base.Use();

		Player.FadeStart();
	}
}
