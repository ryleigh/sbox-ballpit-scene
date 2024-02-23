using Sandbox;

public class PlayerInnerHitbox : Component, Component.ITriggerListener
{
	[Property] public PlayerController Player { get; set; }

	protected override void OnUpdate()
	{

	}

	public void OnTriggerEnter( Collider other )
	{
		if ( IsProxy || Player.IsDead || Player.IsSpectator )
			return;

		if ( other.GameObject.Tags.Has( "ball" ) )
		{
			var ball = other.Components.Get<Ball>();

			if ( ball.IsActive && ball.PlayerNum != Player.PlayerNum )
			{
				Player.HitOpponentBall( ball );
			}
		}
	}

	public void OnTriggerExit( Collider other )
	{
		
	}
}
