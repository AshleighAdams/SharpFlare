
using System;
using SharpFlare.Logger;


namespace SharpFlare
{
	static public class Program
	{
		const uint Major = 0, Minor = 0;

		[CLI.Option("Print version information.", "version", 'v')]
		public static bool Version = false;
		[CLI.Option("Print help information.", "help", 'h')]
		public static bool Help = false;



		static public int Main(string[] args)
		{
			// remove any culture info, everything is invariant; aka en-US.
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

			// load all the stuffs
			// Assembly.LoadFrom(path);

			// parse arguments
			if(!CLI.Options.Parse(args))
				return 1;

			if(Version)
				GlobalLogger.Message(Level.Normal, "SharpFlare v{0}.{1}", Major, Minor);
			if(Help)
				GlobalLogger.Message(Level.Normal, "Help not impl.");
			if(Help || Version)
				return 0;

			SharpFlare.FileSystem.Setup();
			
			Console.WriteLine(FileSystem.LocateFile("/sharpflare"));

			Instancable i = new Instancable();
			i.Hook();
			Hooks.Hook.Call("Test", "hello");
			i.Unhook();
			Hooks.Hook.Call("Test", "hello");
			
			return 0;
		}

	}

	public class Instancable() : Hooks.Hookable
	{
		[Hooks.Hook("Test")]
		public bool Test(params object[] args)
		{
			Console.WriteLine("abc");
			return false;
		}
		
		[Hooks.Hook("Test")]
		public bool Test2(params object[] args)
		{
			Console.WriteLine("xyz");
			return false;
		}
	}
}

/*

public namespace Port
{
	[Site(domain: "abc.com", port: 80, subdomains: true)]
	public class Portfo()
	{
		[Page("/")]
		public Response Index(Request, )
		{
			return Response.Create(HTTP.Okay, "hello");
		}

		[PageRegex("/hello/([A-z0-9]+)")]
		public Response Index(Request, string name)
		{
			return Response.Create(HTTP.Okay, "hello " + name);
		}
	}
}
// */
