using Sandbox;

public sealed class FallingShadow : Component
{
	[Property] public ModelRenderer Renderer { get; set; }

	public TimeSince TimeSinceSpawn { get; set; }
	public float Lifetime = 2.5f;

	public float Scale { get; set; }

	public const float HEIGHT = 100f;

	protected override void OnStart()
	{
		base.OnStart();

		TimeSinceSpawn = 0f;
	}

	protected override void OnUpdate()
	{
		Renderer.Tint = Color.Black.WithAlpha( Utils.Map( TimeSinceSpawn, 0f, Lifetime, 0f, 0.75f, EasingType.QuadIn ) );

		Transform.Scale = Utils.Map( TimeSinceSpawn, 0f, Lifetime, 0.15f, Scale, EasingType.QuadIn );

		if ( TimeSinceSpawn > Lifetime )
		{
			Manager.Instance.SpawnExplosion( (Vector2)Transform.Position, Scale * 1.1f );
			GameObject.Destroy();
		}
	}
}
