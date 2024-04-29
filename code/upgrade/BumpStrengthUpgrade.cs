using Sandbox;
using System;

public class BumpStrengthUpgrade : Upgrade
{
	public static float GetIncrease( int level ) => Utils.Map( level, 0, 9, 4f, 16f );

}
