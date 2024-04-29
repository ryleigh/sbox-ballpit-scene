using Sandbox;
using System;

public class BackstabUpgrade : Upgrade
{
	public static float GetChance( int level ) => Utils.Map( level, 0, 6, 0.1f, 0.9f );

}
