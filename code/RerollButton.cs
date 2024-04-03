using Sandbox;

public class RerollButton : Component
{
	[Sync] public int PlayerNum { get; set; }

	private ModelRenderer _renderer;

	[Broadcast]
	public void Init( int playerNum )
	{
		_renderer = Components.Get<ModelRenderer>();

		if ( IsProxy )
			return;

		PlayerNum = playerNum;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		var player = Manager.Instance.GetPlayer( PlayerNum );
		if ( player == null )
			return;

		_renderer.Tint = player.Money >= player.CurrRerollPrice ? new Color( 0.11f, 0.11f, 0.11f ) : new Color( 0.11f, 0.07f, 0.07f );
	}

	[Broadcast]
	public void DestroyButton()
	{
		if ( IsProxy )
			return;

		GameObject.Destroy();
	}
}
