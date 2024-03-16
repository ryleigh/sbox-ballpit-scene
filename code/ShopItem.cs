using Sandbox;

public class ShopItem : Component
{
	[Sync] public UpgradeType UpgradeType { get; set; }
	[Sync] public int NumLevels { get; set; }
	[Sync] public int Price { get; set; }
	[Sync] public int PlayerNum { get; set; }

	//protected override void OnUpdate()
	//{
	//	Gizmo.Draw.Color = Color.White;
	//	Gizmo.Draw.Text( $"{NumLevels}\n${Price}", new global::Transform( Transform.Position + new Vector3( 0f, 20f, 1f ) ) );
	//}

	[Broadcast]
	public void Init(UpgradeType upgradeType, int numLevels, int price, int playerNum)
	{
		if ( IsProxy )
			return;

		UpgradeType = upgradeType;
		NumLevels = numLevels;
		Price = price;
		PlayerNum = playerNum;
	}

	[Broadcast]
	public void DestroyButton()
	{
		if ( IsProxy )
			return;

		GameObject.Destroy();
	}
}
