using Sandbox;

public class RerollButton : Component
{
	[Broadcast]
	public void DestroyButton()
	{
		if ( IsProxy )
			return;

		GameObject.Destroy();
	}
}
