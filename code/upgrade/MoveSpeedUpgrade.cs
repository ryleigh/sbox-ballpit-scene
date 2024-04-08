using Sandbox;
using System;

public class MoveSpeedUpgrade : Upgrade
{
	public static float GetIncrease( int level ) => Utils.Map( level, 0, 9, 1f, 1.5f, EasingType.SineOut );
}
