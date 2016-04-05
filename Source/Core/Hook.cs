using System;

namespace SharpFlare
{
	namespace Hooks
	{
		[AttributeUsage(AttributeTargets.Method)]
		public class HookAttribute(string name, string id, double order = 0)
			: System.Attribute
		{
			public string Name = name;
			public string Id = id;
			public double Order = order;
		}
	}
}
