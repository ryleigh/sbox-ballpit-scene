using Sandbox;
using System.Drawing;

public sealed class TutorialText : Component
{
	private TimeSince _timeSinceSpawn;

	public float Lifetime { get; set; }

	public string Text0 { get; set; }
	public string Text1 { get; set; }
	public float Opacity { get; set; }
	public float Scale { get; set; }

	public void Init( string text0, string text1, float lifetime )
	{
		Text0 = text0;
		Text1 = text1;
		Lifetime = lifetime;
	}

	protected override void OnStart()
	{
		base.OnStart();

		_timeSinceSpawn = 0f;
	}

	protected override void OnUpdate()
	{
		Opacity = Utils.Map( _timeSinceSpawn, 0f, 0.4f, 0f, 1f, EasingType.Linear ) * Utils.Map( _timeSinceSpawn, 0f, Lifetime - 0.1f, 1f, 0.1f, EasingType.ExpoIn ) * Utils.Map( _timeSinceSpawn, Lifetime - 0.3f, Lifetime - 0.05f, 1f, 0f, EasingType.Linear );
		Scale = Utils.Map( _timeSinceSpawn, 0f, 0.25f, 1.2f, 1f, EasingType.QuadIn ) * Utils.Map( _timeSinceSpawn, 0f, Lifetime, 0.8f, 1.2f, EasingType.QuadOut );

		if ( _timeSinceSpawn > Lifetime )
			GameObject.Destroy();
	}
}
