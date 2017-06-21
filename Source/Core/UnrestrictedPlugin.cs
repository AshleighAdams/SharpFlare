using System;
using System.IO;
using System.Reflection;

namespace SharpFlare
{
	public class UnrestrictedPlugin
	{
		object instance;
		MethodInfo load, unload;

		public UnrestrictedPlugin(string path)
		{
			var asm = Assembly.LoadFile(path);
			var name = Path.GetFileNameWithoutExtension(path).Substring("Plugin.".Length).Replace(".", " ").Replace(" ", "_");
			var t = asm.GetType(name);
			if (t == null)
				throw new Exception($"Could not load plugin {path}: Could not find type {name}");

			instance = Activator.CreateInstance(t);
			load = t.GetMethod("Load");
			unload = t.GetMethod("Unload");

			load.Invoke(instance, new object[0]);
		}
	}
}