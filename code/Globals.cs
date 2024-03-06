global using Sandbox;
global using System;
global using System.Collections.Generic;
global using System.Linq;

public enum UpgradeType { None, MoveSpeed, ShootBalls, }

public class Globals
{
	public const float SFX_HEIGHT = 200f;
	public static string GetNameForUpgrade(UpgradeType upgradeType)
	{
		switch(upgradeType)
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "Move Speed";
			case UpgradeType.ShootBalls: return "Bounce Speed";
		}
	}

	public static string GetDescriptionForUpgrade( UpgradeType upgradeType )
	{
		switch ( upgradeType )
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "Move faster.";
			case UpgradeType.ShootBalls: return "Shoot some balls.";
		}
	}

	public static string GetIconForUpgrade( UpgradeType upgradeType )
	{
		switch ( upgradeType )
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "movespeed";
			case UpgradeType.ShootBalls: return "shoot";
		}
	}

	public static bool IsUpgradePassive( UpgradeType upgradeType )
	{
		switch ( upgradeType )
		{
			case UpgradeType.None: default: return false;
			case UpgradeType.MoveSpeed: return true;
			case UpgradeType.ShootBalls: return false;
		}
	}
}
