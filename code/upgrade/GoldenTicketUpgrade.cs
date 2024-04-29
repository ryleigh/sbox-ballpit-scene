using Sandbox;
using System;

public class GoldenTicketUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Player.SetStat( PlayerStat.GoldenTicketActive, 1f );
		//Manager.Instance.RerollShopItemsLegendary( Player.PlayerNum );
	}

	public override void OnGamePhaseChange( GamePhase oldPhase, GamePhase newPhase )
	{
		base.OnGamePhaseChange( oldPhase, newPhase );

		if ( oldPhase == GamePhase.BuyPhase )
			Player.ClearStat( PlayerStat.GoldenTicketActive );
	}
}
