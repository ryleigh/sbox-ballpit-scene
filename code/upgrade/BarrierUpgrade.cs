using Sandbox;
using System;
using System.Threading.Tasks;

public class BarrierUpgrade : Upgrade
{
	private int _barrierCount; // to handle multiple asyncs at once

	public override void Use()
	{
		base.Use();

		//Manager.Instance.PlaySfx( "barrier", new Vector3( 230f * (Player.PlayerNum == 0 ? -1f : 1f), 0f, 0f ), pitch: Game.Random.Float(1.2f, 1.25f) );

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
			//Manager.Instance.PlaySfx( "barrier", new Vector3( 230f * (Player.PlayerNum == 0 ? -1f : 1f), 0f, 0f ), pitch: Game.Random.Float( 0.85f, 0.9f ) );
		}
	}
}
