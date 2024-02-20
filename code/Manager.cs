using Sandbox;

public sealed class Manager : Component
{
	public static Manager Instance { get; private set; }

	public const float X_FAR = 203f;
	public const float X_CLOSE = 14f;

	public const float Y_LIMIT = 103.7f;

	protected override void OnAwake()
	{
		base.OnAwake();

		Instance = this;
	}

	protected override void OnUpdate()
	{

	}
}
