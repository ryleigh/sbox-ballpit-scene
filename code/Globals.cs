global using Sandbox;
global using System;
global using System.Collections.Generic;
global using System.Linq;

public class Globals
{
	public const float SFX_HEIGHT = 200f;

	public static int GetOpponentPlayerNum(int playerNum)
	{
		return playerNum == 0 ? 1 : 0;
	}
}
