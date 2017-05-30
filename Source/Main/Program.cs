
using System;
using SharpFlare.Logger;
using System.Threading.Tasks;

using SharpFlare.Http;
using System.Text;
using System.IO;

namespace SharpFlare
{
	static public class Program
	{
		const uint Major = 0, Minor = 0;

		[CLI.Option("Print version information.", "version", 'v')]
		public static bool Version = false;
		[CLI.Option("Print help information.", "help", 'h')]
		public static bool Help = false;

		static string loremipsum = 
			"<html>" +
			"	<head>" +
			"		<title>Testing</title>" +
			"	</head>" +
			"	<body>" +
			"		Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed tempus euismod commodo. Aenean eu leo sed tellus eleifend iaculis. Vestibulum a tortor condimentum, rhoncus metus non, molestie tellus. Ut sit amet orci rhoncus, consequat nisi nec, finibus nisi. Pellentesque laoreet lacus vel urna auctor mollis. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Cras eget nunc congue, mattis dui ac, egestas tortor. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam blandit nunc vitae ipsum rutrum, a scelerisque enim scelerisque. Pellentesque id sagittis elit, nec eleifend turpis. Nulla ex elit, sollicitudin id turpis id, congue accumsan augue. Vivamus quis nibh ac metus tincidunt pellentesque. Fusce vitae libero et dolor pharetra mollis." +
			"	</body>" +
			"</html>";

		private static async Task<bool> HandleRequest(params object[] args)
		{
			Request req = (Request)args[0];
			Response res = (Response)args[1];

			res["Content-Type"] = "text/html";
			res.Content = new MemoryStream(Encoding.UTF8.GetBytes(loremipsum));
			await res.Finalize();

			return false;
		}

		static public int Main(string[] args)
		{
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			// load all the stuffs
			// Assembly.LoadFrom(path);
			// parse arguments
			if (!CLI.Options.Parse(args))
				return 1;
			
			if(Version)
				GlobalLogger.Message(Level.Normal, "SharpFlare v{0}.{1}", Major, Minor);
			if(Help)
				GlobalLogger.Message(Level.Normal, "Help not impl.");
			if(Help || Version)
				return 0;

			SharpFlare.FileSystem.Setup();
			
			Console.WriteLine(FileSystem.LocateFile("/sharpflare.exe"));

			Instancable i = new Instancable();
			i.Hook();
			Hooks.Hook.Call("Test", "hello").Wait();
			i.Unhook();
			Hooks.Hook.Call("Test", "hello").Wait();

			// int tp, atp;
			// System.Threading.ThreadPool.GetMinThreads(out tp, out atp);
			// System.Threading.ThreadPool.SetMinThreads(tp*4, atp*4);
			// Console.WriteLine(tp);Console.WriteLine(atp);

			Hooks.Hook.Add("Request", "Main", HandleRequest);

			HttpListener.Listen(8080);

			while(true)
			{
				//Console.ReadLine();
				Task.Delay(-1).Wait();
			}

			return 0;
		}
		
	}

	public class Instancable : Hooks.Hookable
	{
		public Instancable()
		{
		}

		[Hooks.Hook("Test")]
		public async Task<bool> Test(params object[] args)
		{
			Console.WriteLine("abc");
			return false;
		}
		
		[Hooks.Hook("Test")]
		public async Task<bool> Test2(params object[] args)
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
