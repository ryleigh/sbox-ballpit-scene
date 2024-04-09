using Sandbox;
using System;
using System.Threading.Tasks;

public class FadeUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Manager.PlaySfx( "bubble", Player.Transform.Position );

		Player.FadeStart();
	}
}
