using Sandbox;
using System;
using System.Threading.Tasks;

public class BarrierUpgrade : Upgrade
{
	public override string SfxUse => "barrier_use";

	private int _barrierCount; // to handle multiple asyncs at once

	public override void Use()
	{
		base.Use();

		//Manager.Instance.PlaySfx( "barrier", new Vector3( 230f * (Player.PlayerNum == 0 ? -1f : 1f), 0f, 0f ), pitch: Game.Random.Float(1.2f, 1.25f) );

		BarrierAsync();
	}

	// todo: handle when someone forfeits/leaves lobby while barrier active
	// todo: handle when someone joins and barrier is active during snapshot?
	async void BarrierAsync()
	{
		Player.IsBarrierActive = true;
		Player.SetBarrierVisible( true );
		_barrierCount++;

		await Task.Delay( 2500 );

		if ( Player == null || Manager.Instance.GamePhase == GamePhase.WaitingForPlayers )
			return;

		_barrierCount--;
		if(_barrierCount <= 0)
		{
			Player.IsBarrierActive = false;
			Player.SetBarrierVisible( false );

			Manager.Instance.PlaySfx( "barrier_end", new Vector3( 230f * (Player.PlayerNum == 0 ? -1f : 1f), 0f, 0f ), pitch: Game.Random.Float( 0.85f, 0.9f ) );
		}
	}
}
