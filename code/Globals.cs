global using Sandbox;
global using System;
global using System.Collections.Generic;
global using System.Linq;

public enum UpgradeType { None, MoveSpeed, Volley, Gather, }

public class Globals
{
	public const float SFX_HEIGHT = 200f;
	public static string GetNameForUpgrade(UpgradeType upgradeType)
	{
		switch(upgradeType)
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "Move Speed";
			case UpgradeType.Volley: return "Volley";
			case UpgradeType.Gather: return "Gather";
		}
	}

	public static string GetIconForUpgrade( UpgradeType upgradeType )
	{
		switch ( upgradeType )
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "🏃🏻";
			case UpgradeType.Volley: return "🔴";
			case UpgradeType.Gather: return "⤵️";
		}
	}

	public static string GetDescriptionForUpgrade( UpgradeType upgradeType )
	{
		switch ( upgradeType )
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "Move faster.";
			case UpgradeType.Volley: return "Shoot some balls.";
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
			case UpgradeType.Volley: return false;
			case UpgradeType.Gather: return false;
		}
	}

	public static int GetOpponentPlayerNum(int playerNum)
	{
		return playerNum == 0 ? 1 : 0;
	}
}
