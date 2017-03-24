using System;
using System.Reflection;
using System.IO;

namespace SharpFlare
{
	public interface Plugin
	{
		bool Load();
		bool Unload();
	}
}
