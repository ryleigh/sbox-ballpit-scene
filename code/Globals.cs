global using Sandbox;
global using System;
global using System.Collections.Generic;
global using System.Linq;

public class Globals
{
	public static string GetNameForUpgrade(UpgradeType upgradeType)
	{
		switch(upgradeType)
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "Move Speed";
			case UpgradeType.BallBounceSpeed: return "Bounce Speed";
		}
	}

	public static string GetDescriptionForUpgrade( UpgradeType upgradeType )
	{
		switch ( upgradeType )
		{
			case UpgradeType.None: default: return "";
			case UpgradeType.MoveSpeed: return "Move faster.";
			case UpgradeType.BallBounceSpeed: return "Increase speed of your balls when you bounce them.";
		}
	}
}
