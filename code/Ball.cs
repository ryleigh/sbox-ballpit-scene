using Sandbox;

public class Ball : Component
{
	public int PlayerNum { get; set; }

	public Vector2 Velocity { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		Velocity = (new Vector2( Game.Random.Float( -1f, 1f ), Game.Random.Float( -1f, 1f ) )).Normal * 100f;
	}

	protected override void OnUpdate()
	{
		
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsProxy )
			return;

		Transform.Position += (Vector3)Velocity * Time.Delta;

		CheckBounds();
	}

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
}
