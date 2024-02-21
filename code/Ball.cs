using Sandbox;

public class Ball : Component
{
	public int PlayerNum { get; set; }
	//[Sync] public int CurrentSide { get; set; }

	[Sync] public Vector2 Velocity { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
			return;

		Velocity = (new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) )).Normal * 100f;
	}

	protected override void OnUpdate()
	{
		//if(Network.OwnerConnection != null)
		//{
		//	Gizmo.Draw.Color = Color.Black.WithAlpha( 0.75f );
		//	Gizmo.Draw.Text( $"{Network.OwnerConnection.DisplayName}", new global::Transform( Transform.Position ) );

		//	Gizmo.Draw.Color = Color.White.WithAlpha( 0.75f );
		//	Gizmo.Draw.Text( $"{Network.OwnerConnection.DisplayName}", new global::Transform( Transform.Position + new Vector3(0f, 1f, 1f)) );
		//}

		if ( IsProxy )
			return;

		Transform.Position += (Vector3)Velocity * Time.Delta;

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
		Components.Get<ModelRenderer>().Tint = playerNum == 0 ? Manager.Instance.ColorPlayer0 : Manager.Instance.ColorPlayer1;
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
}
