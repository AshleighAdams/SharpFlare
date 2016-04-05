using System;
using System.Reflection;
using System.IO;

namespace SharpFlare
{
	public static class Plugins
	{
		public enum SearchPath
		{
			Plugin,       // /?
			Binary,     // plugin/bin/?
			Static, // plugin/static/?
		}

		static string GetPluginPath(Assembly caller)
		{
			UriBuilder uri = new UriBuilder(caller.CodeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			return Directory.GetParent(Directory.GetParent(path).FullName).FullName;
		}

		public static string LocateFile(SearchPath search, string path, bool globbed = false) // globs are like /*.jpg
		{
			string libpath = GetPluginPath(Assembly.GetCallingAssembly());
			string sp = "/";

			switch(search)
			{
			case SearchPath.Binary: sp = "/bin";    break;
			case SearchPath.Static: sp = "/static"; break;
			default:
			case SearchPath.Plugin: sp = "/.";      break;
			}

			return libpath + sp + path;
		}
	}
}
