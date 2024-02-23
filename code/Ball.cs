using Sandbox;

public class Ball : Component
{
	public int PlayerNum { get; set; }
	//[Sync] public int CurrentSide { get; set; }

	[Sync] public Vector2 Velocity { get; set; }
	[Property, Sync, Hide] public Color Color { get; private set; }

	//public HighlightOutline HighlightOutline { get; private set; }
	public ModelRenderer ModelRenderer { get; private set; }

	[Sync] public bool IsActive { get; set; }

	public bool IsDespawning { get; private set; }
	public TimeSince TimeSinceDespawnStart { get; private set; }
	private float _despawnTime = 3f;

	protected override void OnAwake()
	{
		base.OnAwake();

		//HighlightOutline = Components.Get<HighlightOutline>();
		ModelRenderer = Components.Get<ModelRenderer>();
	}

	protected override void OnStart()
	{
		base.OnStart();

		IsActive = true;

		if ( IsProxy )
			return;

		//Velocity = (new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) )).Normal * 100f;
	}

	protected override void OnUpdate()
	{
		//Gizmo.Draw.Color = Color.White.WithAlpha( 0.75f );
		//Gizmo.Draw.Text( $"{Color}", new global::Transform( Transform.Position + new Vector3(0f, 1f, 1f)) );

		if ( IsDespawning)
		{
			if ( ModelRenderer != null )
			{
				ModelRenderer.Tint = Color.Lerp( Color, Color.WithAlpha(0f), Utils.Map(TimeSinceDespawnStart, 0f, _despawnTime, 0f, 1f) );
			}
		}
		else
		{
			Transform.Position += (Vector3)Velocity * Time.Delta;

			if ( ModelRenderer != null )
				ModelRenderer.Tint = Color.WithAlpha( Utils.Map( Utils.FastSin( PlayerNum * 16f + Time.Now * 8f ), -1f, 1f, 0.8f, 1.2f, EasingType.SineInOut ) );
		}

		//if(HighlightOutline != null)
		//	HighlightOutline.Width = 0.2f + Utils.FastSin(Time.Now * 16f) * 0.05f;

		if ( IsProxy )
			return;

		if ( IsDespawning )
		{
			if ( TimeSinceDespawnStart > _despawnTime )
			{
				GameObject.Destroy();
				return;
			}
		}

		CheckBounds();

		//if(CurrentSide == 0 && Transform.Position.x > 0f)
		//{
		//	SetSide( 1 );
		//}
		//else if(CurrentSide == 1 && Transform.Position.x < 0f)
		//{
		//	SetSide( 0 );
		//}
	}

	//protected override void OnFixedUpdate()
	//{
	//	base.OnFixedUpdate();

	//	if ( IsProxy )
	//		return;
		
	//}

	void CheckBounds()
	{
		var xMin = -Manager.X_FAR + (PlayerNum == 0 ? -10f : 0f);
		var xMax = Manager.X_FAR + (PlayerNum == 1 ? 10f : 0f);
		var yMin = -Manager.Y_LIMIT - 8f;
		var yMax = Manager.Y_LIMIT + 8f;

		if ( Transform.Position.x < xMin )
		{
			if ( PlayerNum == 0 )
			{
				GameObject.Destroy();
				return;
			}
			else
			{
				Transform.Position = Transform.Position.WithX( xMin );
				Velocity = Velocity.WithX( MathF.Abs( Velocity.x ) );
			}
		}
		else if ( Transform.Position.x > xMax )
		{
			if ( PlayerNum == 0 )
			{
				Transform.Position = Transform.Position.WithX( xMax );
				Velocity = Velocity.WithX( -MathF.Abs( Velocity.x ) );
			}
			else
			{
				GameObject.Destroy();
				return;
			}
		}

		if ( Transform.Position.y < yMin )
		{
			Velocity = Velocity.WithY( MathF.Abs( Velocity.y) );
		}
		else if ( Transform.Position.y > yMax )
		{
			Velocity = Velocity.WithY( -MathF.Abs( Velocity.y ) );
		}
	}

	[Broadcast]
	public void SetPlayerNum(int playerNum )
	{
		PlayerNum = playerNum;

		Color = playerNum == 0 ? Manager.Instance.ColorPlayer0 : Manager.Instance.ColorPlayer1;
		Components.Get<ModelRenderer>().Tint = Color;

		//var highlightOutline = Components.Get<HighlightOutline>();
		//highlightOutline.Width = 0.2f;
		//highlightOutline.Color = Color;
		//highlightOutline.InsideColor = Color.WithAlpha( 0.75f );
	}

	//public void SetSide(int side)
	//{
	//	if(CurrentSide == side) 
	//		return;

	//	CurrentSide = side;

	//	var connection = Manager.Instance.GetConnection( side );
	//	if( connection != null )
	//	{
	//		Network.AssignOwnership( connection );
	//	}
	//}

	[Broadcast]
	public void HitPlayer(Guid hitPlayerId)
	{
		if ( IsProxy )
			return;

		GameObject.Destroy();
	}

	[Broadcast]
	public void Despawn()
	{
		if(!IsActive)
			return;

		IsDespawning = true;
		TimeSinceDespawnStart = 0f;

		if ( IsProxy )
			return;

		IsActive = false;
	}
}
