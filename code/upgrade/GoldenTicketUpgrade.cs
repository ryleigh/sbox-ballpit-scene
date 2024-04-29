using Sandbox;
using System;

public class GoldenTicketUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Player.SetStat( PlayerStat.GoldenTicketActive, 1f );
		Manager.Instance.RerollShopItems( Player.PlayerNum, increasePrice: false );
	}

	public override void OnGamePhaseChange( GamePhase oldPhase, GamePhase newPhase )
	{
		base.OnGamePhaseChange( oldPhase, newPhase );

		if ( oldPhase == GamePhase.BuyPhase )
			Player.ClearStat( PlayerStat.GoldenTicketActive );
	}
}
