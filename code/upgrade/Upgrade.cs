using Sandbox;
using System;

public class Upgrade
{
	public PlayerController Player { get; private set; }
	public Manager Manager { get; private set; }
	public Scene Scene { get; private set; }
	public int Level { get; set; }
	public bool IsPassive { get; private set; }

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

	}

	public virtual void ClearProgress()
	{

	}
}
