global using Sandbox;
global using System;
global using System.Collections.Generic;
global using System.Linq;

public enum UpgradeType { None, MoveSpeed, ShootBalls, Gather, }

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
			case UpgradeType.Gather: return "Gather";
		}
	}

	public static string GetIconForUpgrade( UpgradeType upgradeType )
	{
		switch ( upgradeType )
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "🏃🏻";
			case UpgradeType.ShootBalls: return "🔴";
			case UpgradeType.Gather: return "⤵️";
		}
	}

	public static string GetDescriptionForUpgrade( UpgradeType upgradeType )
	{
		switch ( upgradeType )
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "Move faster.";
			case UpgradeType.ShootBalls: return "Shoot some balls.";
			case UpgradeType.Gather: return "Your balls target you.";
		}
	}

	//public static string GetIconForUpgrade( UpgradeType upgradeType )
	//{
	//	switch ( upgradeType )
	//	{
	//		case UpgradeType.None: default: return "";
	//		case UpgradeType.MoveSpeed: return "movespeed";
	//		case UpgradeType.ShootBalls: return "shoot";
	//		case UpgradeType.Gather: return "gather";
	//	}
	//}

	public static bool IsUpgradePassive( UpgradeType upgradeType )
	{
		switch ( upgradeType )
		{
			case UpgradeType.None: default: return false;
			case UpgradeType.MoveSpeed: return true;
			case UpgradeType.ShootBalls: return false;
			case UpgradeType.Gather: return false;
		}
	}
}
