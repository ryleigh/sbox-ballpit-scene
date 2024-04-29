using Sandbox;
using System;

public class Upgrade
{
	public PlayerController Player { get; private set; }
	public Manager Manager { get; private set; }
	public Scene Scene { get; private set; }
	public int Level { get; set; }
	public bool IsPassive { get; private set; }

	public virtual string SfxGet => "bubble";
	public virtual string SfxUse => "bubble";
	public virtual string SfxSelect => "bubble_ui";

	public virtual void Init(PlayerController player, Manager manager, Scene scene, bool isPassive)
	{
		Player = player;
		Manager = manager;
		Scene = scene;
		IsPassive = isPassive;
	}

	public virtual void Update(float dt)
	{

	}

	public virtual void SetLevel(int newLevel)
	{
		Level = newLevel;
	}

	public virtual void Remove()
	{

	}

	public virtual void Use()
	{
		if(!string.IsNullOrEmpty(SfxUse))
			Manager.Instance.PlaySfx( SfxUse, Player.Transform.Position );
	}

	public virtual void ClearProgress()
	{

	}

	public virtual void OnGamePhaseChange(GamePhase oldPhase, GamePhase newPhase)
	{

	}

	public virtual void BumpOwnBall(Ball ball)
	{

	}
}
