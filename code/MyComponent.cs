using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class MyComponent : Component
{
	[Property] public string StringProperty { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

	}

	protected override void OnUpdate()
	{
	}
}
