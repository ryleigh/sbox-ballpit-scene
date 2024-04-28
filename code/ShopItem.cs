using Sandbox;

public class ShopItem : Component
{
	[Sync] public UpgradeType UpgradeType { get; set; }
	[Sync] public int NumLevels { get; set; }
	[Sync] public int Price { get; set; }
	[Sync] public int PlayerNum { get; set; }

	public float BobSpeed { get; private set; }
	public float ShadowDistance { get; private set; }
	public float ShadowBlur { get; private set; }
	public float ShadowOpacity { get; private set; }
	public Vector2 IconOffset { get; private set; }
	private float _timingOffset;

	protected override void OnAwake()
	{
		base.OnAwake();

		BobSpeed = 2.75f;
		_timingOffset = Game.Random.Float( 0f, 5f );
	}

	protected override void OnUpdate()
	{
		var player = Manager.Instance.GetPlayer( PlayerNum );
		var playerLevel = player?.GetUpgradeLevel( UpgradeType ) ?? 0;
		var maxLevel = Manager.Instance.GetMaxLevelForUpgrade( UpgradeType );
		var isMaxLevel = playerLevel >= maxLevel;

		var money = player?.Money ?? 0;
		var cannotAfford = money < Price;

		var bobSpeed = BobSpeed * (isMaxLevel || cannotAfford ? 0.5f : 1f);

		IconOffset = new Vector2( 0f, Utils.FastSin( _timingOffset + Time.Now * bobSpeed ) * 3.5f );
		ShadowDistance = 9f + Utils.FastSin( _timingOffset + Time.Now * bobSpeed ) * 3f;
		ShadowBlur = 2.3f + Utils.FastSin( _timingOffset + Time.Now * bobSpeed ) * 1.2f;
		ShadowOpacity = 0.85f - Utils.FastSin( _timingOffset + Time.Now * bobSpeed ) * 0.15f;
	}

	[Broadcast]
	public void Init(UpgradeType upgradeType, int numLevels, int price, int playerNum)
	{
		//if ( IsProxy )
		//	return;

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
