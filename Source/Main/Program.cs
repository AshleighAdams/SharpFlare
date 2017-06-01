
using System;
using SharpFlare.Logger;
using System.Threading.Tasks;

using SharpFlare.Http;
using System.Text;
using System.IO;
using static SharpFlare.Router;
using System.Net;
using System.Text.RegularExpressions;

namespace SharpFlare
{
	static public class Program
	{
		const uint Major = 0, Minor = 0;

		[CLI.Option("Print version information.", "version", 'v')]
		public static bool Version = false;
		[CLI.Option("Print help information.", "help", 'h')]
		public static bool Help = false;

		static int Test()
		{
			Console.WriteLine(FileSystem.LocateFile("/sharpflare.exe"));
			Instancable i = new Instancable();
			i.Hook();
			Hooks.Hook.Call("Test", "hello").Wait();
			i.Unhook();
			Hooks.Hook.Call("Test", "hello").Wait();
			return 0;
		}

		static string loremipsum =
			"<html>\n" +
			"	<head>\n" +
			"		<title>Testing</title>\n" +
			"	</head>\n" +
			"	<body>\n" +
			"		Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed tempus euismod commodo. Aenean eu leo sed tellus eleifend iaculis. Vestibulum a tortor condimentum, rhoncus metus non, molestie tellus. Ut sit amet orci rhoncus, consequat nisi nec, finibus nisi. Pellentesque laoreet lacus vel urna auctor mollis. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Cras eget nunc congue, mattis dui ac, egestas tortor. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam blandit nunc vitae ipsum rutrum, a scelerisque enim scelerisque. Pellentesque id sagittis elit, nec eleifend turpis. Nulla ex elit, sollicitudin id turpis id, congue accumsan augue. Vivamus quis nibh ac metus tincidunt pellentesque. Fusce vitae libero et dolor pharetra mollis.\n" +
			"	</body>\n" +
			"</html>\n";

		public static string MethodThatIsBroken(string[] args)
		{
			return args[10];
		}

		public static async Task Lorem(Request req, Response res, string[] args)
		{
			string x = MethodThatIsBroken(args); // out of bounds
			res["Content-Type"] = "text/html";
			res.Content = new MemoryStream(Encoding.UTF8.GetBytes(loremipsum));
			await res.Finalize();
		}

		static public int Main(string[] args)
		{
			System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
			// load all the stuffs
			// Assembly.LoadFrom(path);
			// parse arguments
			if (!CLI.Options.Parse(args))
				return 1;

			// recover the source location to remove it later on
			try { throw new Exception(); } catch(Exception ex)
			{
				string path = Regex.Match(ex.StackTrace.Split('\n')[0].Trim(), "in (.*):line").Groups[1].Value;
				string[] parts = path.Split('/', '\\');
				path = string.Join("/", parts, 0, parts.Length - 2);
				Util.SourceCodeBase = path;
			}

			if(Version)
				GlobalLogger.Message(Level.Normal, "SharpFlare v{0}.{1}", Major, Minor);
			if(Help)
				GlobalLogger.Message(Level.Normal, "Help not impl.");
			if(Help || Version)
				return 0;

			SharpFlare.FileSystem.Setup();
			
			Hooks.Hook.Add("Request", "Main", Router.HandleRequest);
			Host.Any.Route("/lorem", Lorem);
			DefaultErrorHandler.Setup();

			Task ipv4 = HttpListener.ListenAsync(8080, IPAddress.Any);
			Task ipv6 = HttpListener.ListenAsync(8080, IPAddress.IPv6Any);

			Task.WaitAll(ipv4, ipv6);

			//while (true)
			//	Task.Delay(-1).Wait();
			
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
