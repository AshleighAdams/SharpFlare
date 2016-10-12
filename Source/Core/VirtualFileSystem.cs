using System;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;

using SharpFlare.Logger;


namespace SharpFlare
{
	class VFSContext
	{
		public LinkedList<string> SearchPaths = new LinkedList<string>();
		public bool Promotable = true; // can we go to root?
	}
	
	public static class FileSystem
	{
		static VFSContext RootContext = new VFSContext();
		static Dictionary<Assembly, VFSContext> Contexts = new Dictionary<Assembly, VFSContext>();
		
		public static void Setup()
		{
			string binpath = AppDomain.CurrentDomain.BaseDirectory;
			string sharedpath = Directory.GetCurrentDirectory();
			
			RootContext.SearchPaths.AddLast(binpath.Substring(0, binpath.Length - 1));
			RootContext.SearchPaths.AddLast(sharedpath);
		}
		
		public static string LocateFile(string path, bool root = false)
		{
			VFSContext vfs;
			if(root)
				vfs = RootContext;
			else if(!Contexts.TryGetValue(Assembly.GetCallingAssembly(), out vfs))
				return LocateFile(path, true);
			
			foreach(string spath in Enumerable.Reverse(vfs.SearchPaths))
			{
				
				string fullpath = $"{spath}{path}";
				Console.WriteLine($"checking {spath} for {path}");
				if(File.Exists(fullpath))
					return fullpath;
			}
			
			if(!root && vfs.Promotable)
				return LocateFile(path, true);
			throw new FileNotFoundException(path);
		}
	}
}
