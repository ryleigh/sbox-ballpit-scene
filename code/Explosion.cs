using Sandbox;

public class Explosion : Component
{
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public SphereCollider SphereCollider { get; set; }

	private Color _colorA;
	private Color _colorB;

	public TimeSince TimeSinceSpawn { get; set; }
	public float Lifetime = 0.4f;

	public float Scale { get; set; }

	public bool DealtDamage { get; set; }

	private bool _hasRepelledBalls;

	protected override void OnStart()
	{
		base.OnStart();

		_colorA = new Color( 0.8f, 0f, 0f );
		_colorB = new Color( 1f, 1f, 0f );
		TimeSinceSpawn = 0f;
	}

	protected override void OnUpdate()
	{
		Renderer.Tint = Color.Lerp( _colorA, _colorB, Utils.FastSin(TimeSinceSpawn * 32f) ).WithAlpha( Utils.Map( TimeSinceSpawn, 0f, Lifetime, 1000f, 0f, EasingType.ExpoOut ) );
		
		Transform.Scale = Utils.Map( TimeSinceSpawn, 0f, Lifetime, Scale, Scale * 1.25f, EasingType.QuadIn ) * Utils.MapReturn( TimeSinceSpawn, 0f, 0.1f, 1.3f, 0.7f, EasingType.QuadOut);

		if ( !_hasRepelledBalls && TimeSinceSpawn > 0.05f )
		{
			var explosionRadius = SphereCollider.Radius;

			foreach ( var ball in Scene.GetAllComponents<Ball>() )
			{
				if ( !ball.IsActive )
					continue;

				var distSqr = ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).LengthSquared;
				if ( distSqr < MathF.Pow( explosionRadius, 2f ) )
				{
					var speed = ball.Velocity.Length * 1.1f;
					var dir = ((Vector2)ball.Transform.Position - (Vector2)Transform.Position).Normal;
					ball.SetVelocity( dir * speed, timeScale: 0f, duration: Game.Random.Float(0.13f, 0.2f), EasingType.ExpoIn );
				}
			}

			_hasRepelledBalls = true;
		}

		if ( TimeSinceSpawn > Lifetime )
			GameObject.Destroy();
	}
}
