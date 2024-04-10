using Sandbox;
using System;
using System.Threading.Tasks;

public class EndowUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		Manager.PlaySfx( "bubble", Player.Transform.Position );

		EndowAsync();
	}

	async void EndowAsync()
	{
		for(int i = 0; i < 10; i++)
		{
			await Task.Delay( 125 );

			if ( Manager.Instance.GamePhase == GamePhase.RoundActive && !Player.IsDead && !Player.IsSpectator )
			{
				Manager.SpawnMoneyEndow( Manager.Instance.GetConnection( Player.PlayerNum ), 1, Player.Transform.Position );
				Manager.PlaySfx( "bubble", Player.Transform.Position, volume: 0.3f, pitch: Game.Random.Float(0.8f, 0.95f) );
			}
		}
	}
}
