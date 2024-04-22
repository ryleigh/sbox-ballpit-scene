using Sandbox;
using System;
using System.Threading.Tasks;

public class BarrierUpgrade : Upgrade
{
	private int _barrierCount; // to handle multiple asyncs at once

	public override void Use()
	{
		base.Use();

		BarrierAsync();
	}

	async void BarrierAsync()
	{
		Player.IsBarrierActive = true;
		Player.SetBarrierVisible( true );
		_barrierCount++;

		await Task.Delay( 2500 );

		_barrierCount--;
		if(_barrierCount <= 0)
		{
			Player.IsBarrierActive = false;
			Player.SetBarrierVisible( false );
		}
	}
}
