using Sandbox;

public class SkipButton : Component
{
	[Broadcast]
	public void DestroyButton()
	{
		if ( IsProxy )
			return;

		GameObject.Destroy();
	}
}
