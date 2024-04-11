using Sandbox;
using System;

public class AirstrikeData
{
	public Vector2 pos;
	public int currNumStrikes;
	public int totalNumStrikes = 10;
	public TimeSince timeSinceLastStrike;
	public float delay;
}

public class AirstrikeUpgrade : Upgrade
{
	public override void Use()
	{
		base.Use();

		float BUFFER = 5f;
		Vector2 pos = new Vector2( 
			Math.Clamp( Manager.Instance.MouseWorldPos.x, -Manager.X_FAR + BUFFER, Manager.X_FAR - BUFFER ),
			Math.Clamp( Manager.Instance.MouseWorldPos.y, -Manager.Y_LIMIT + BUFFER, Manager.Y_LIMIT - BUFFER )
		);

		Manager.Instance.SpawnFloaterText(
			new Vector3( pos.x, pos.y, 120f),
			"⚠️",
			lifetime: 1.5f,
			color: Color.White,
			velocity: Vector2.Zero,
			deceleration: 0f,
			startScale: 0.2f,
			endScale: 0.5f,
			isEmoji: true
		);

		Manager.Instance.StartAirstrike(pos);

		Manager.Instance.PlaySfx( "bubble", Player.Transform.Position );
		Manager.Instance.PlaySfx( "warning", new Vector3( pos.x, pos.y, 0f ), volume: 1f, pitch: 0.9f );
	}
}
