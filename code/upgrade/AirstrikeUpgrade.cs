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
	public override string SfxUse => "airstrike_use";

	public override void Use()
	{
		base.Use();

		float BUFFER = 5f;
		Vector2 pos = new Vector2( 
			Math.Clamp( Manager.Instance.MouseWorldPos.x, -Manager.X_FAR + BUFFER, Manager.X_FAR - BUFFER ),
			Math.Clamp( Manager.Instance.MouseWorldPos.y, -Manager.Y_LIMIT + BUFFER, Manager.Y_LIMIT - BUFFER )
		);

		Manager.Instance.StartAirstrike(pos);
	}
}
