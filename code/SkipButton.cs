using Sandbox;

public class SkipButton : Component
{
	[Sync] public bool ShouldFlash { get; set; }

	public bool FlashToggle { get; set; }
	private TimeSince _timeSinceFlash;

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( ShouldFlash)
		{
			if(_timeSinceFlash > (FlashToggle ? 0.1f : 0.15f))
			{
				FlashToggle = !FlashToggle;
				_timeSinceFlash = 0f;
			}
		}
	}

	[Broadcast]
	public void DestroyButton()
	{
		if ( IsProxy )
			return;

		GameObject.Destroy();
	}

	[Broadcast]
	public void StartFlashing()
	{
		ShouldFlash = true;
	}
}
