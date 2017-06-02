
using System;
using SharpFlare.Logger;
using System.Threading.Tasks;

using SharpFlare.Http;
using System.Text;
using System.IO;
using static SharpFlare.Router;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Generic;

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
			Instancable i = new Instancable();
			i.Hook();
			for (int n = 0; n < 1000000; n++)
			{
				Hooks.Call("Test", "hello").Wait();
				Hooks.Call("Test", "hello").Wait();
			}
			i.Unhook();
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
		public static async Task Lorem(Request req, Response res, string[] args)
		{
			res["Content-Type"] = "text/html";
			res.Content = new MemoryStream(Encoding.UTF8.GetBytes(loremipsum));
		}

		public static async Task TestMissingFile(Request req, Response res, string[] args)
		{
			res["Content-Type"] = "text/html";
			res.Content = new FileStream("idontexist.html", FileMode.Open);
		}

		#region ASYNCGEN
		public static string Asyncify(Type type, List<string> importedNamespaces)
		{
			StringBuilder writer = new StringBuilder();

			var TaskType = TypeToCSharp(typeof(Task), importedNamespaces);

			foreach (var method in type.GetMethods())
			{

				if (method.IsStatic & method.IsPublic)
				{
					var parameterList = new List<string>();
					var argList = new List<string>();
					foreach (var parameter in method.GetParameters())
					{
						parameterList.Add(TypeToCSharp(parameter.ParameterType, importedNamespaces) + " " + parameter.Name);
						argList.Add(parameter.Name);

						if (parameter.IsOut | parameter.IsOptional | parameter.IsRetval)
						{
							throw new NotImplementedException();
						}
					}

					var parameters = string.Join(", ", parameterList);
					var args = string.Join(", ", argList);
					var returnType = TypeToCSharp(method.ReturnType, importedNamespaces);
					var typeName = TypeToCSharp(type, importedNamespaces);

					if (method.ReturnType == typeof(void))
					{
						writer.AppendFormat(
							"public static {5} {1}Async({2})\n{{ return {5}.Run(() => {{ {4}.{1}({3}); }}); }}\n",
							returnType, method.Name, parameters, args, typeName, TaskType
						);
					}
					else
					{
						writer.AppendFormat(
							"public static {5}<{0}> {1}Async({2})\n{{ return {5}.Run(() => {{ return {4}.{1}({3}); }}); }}\n",
							returnType, method.Name, parameters, args, typeName, TaskType
						);
					}
				}
			}

			return writer.ToString();
		}

		static string TypeToCSharp(Type type, List<string> importedNamespaces)
		{

			string ns;
			if (importedNamespaces.Contains(type.Namespace))
			{
				ns = "";
			}
			else
			{
				ns = type.Namespace + ".";
			}

			if (!type.IsGenericType)
			{
				return ns + type.Name;
			}
			else
			{
				var genericList = new List<string>();
				foreach (var gType in type.GenericTypeArguments)
				{
					genericList.Add(TypeToCSharp(gType, importedNamespaces));
				}
				var name = type.Name;
				name = name.Substring(0, name.IndexOf("`"));
				return string.Format("{0}{1}<{2}>", ns, name, string.Join(", ", genericList));
			}
		}
		public static async Task GenerateAsyncMethods(Request req, Response res, string[] args)
		{
			var namespaces = new List<string> { "System", "System.Collections.Generic", "System.IO", "System.Threading.Tasks" };
			string code = Asyncify(typeof(System.IO.File), namespaces);
			res["Content-Type"] = "text/plain";
			res.Content = new MemoryStream(Encoding.UTF8.GetBytes(code));
		}
		#endregion

		[LoaderOptimization(LoaderOptimization.MultiDomain)]
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
			
			Hooks.Add("Request", "Main", Router.HandleRequest);
			Host.Any.Route("/lorem", Lorem);
			Host.Any.Route("/testmissing", TestMissingFile);
			Host.Any.Route("/async", GenerateAsyncMethods);
			DefaultErrorHandler.Setup();

			Task ipv4 = HttpListener.ListenAsync(8080, IPAddress.Any);
			Task ipv6 = HttpListener.ListenAsync(8080, IPAddress.IPv6Any);
			
			Plugin plug = new Plugin("TestPlugin.dll");

			Task.WaitAll(ipv4, ipv6);

			//while (true)
			//	Task.Delay(-1).Wait();
			
			return 0;
		}
		
	}

	public class Instancable : Hookable
	{
		public Instancable()
		{
		}

		[Hook("Test")]
		public async Task<bool> Test(params object[] args)
		{
			//Console.WriteLine("abc");
			return false;
		}
		
		[Hook("Test")]
		public async Task<bool> Test2(params object[] args)
		{
			//Console.WriteLine("xyz");
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
