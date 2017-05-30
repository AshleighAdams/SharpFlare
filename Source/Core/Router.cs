using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using SharpFlare.Http;

using PageGenerator = System.Func<SharpFlare.Http.Request, SharpFlare.Http.Response, string[], System.Threading.Tasks.Task>;

namespace SharpFlare
{
	public static class Router
	{
		/*
			*.abc.com
			abc.com
			xyz.*.abc.com
			**.abc.com
		*/
		public class Route
		{
			public readonly string Path;
			public readonly bool Static;
			public readonly PageGenerator Generator;

			public Route(string path, PageGenerator gen)
			{
				Static = true; // TODO:
				Path = path;
				Generator = gen;
			}
		}

		public class Host
		{
			public static readonly Host Any = new Host("*");
			private static Dictionary<string, Host> Hosts = new Dictionary<string, Host>();

			public readonly string Domain;
			public readonly bool Static; // do we need to perform a pattern match?

			public static Host GetHost(string domain)
			{
				Host h;
				if (Hosts.TryGetValue(domain, out h))
					return h;
				h = new Host(domain);
				Hosts[domain] = h;
				return h;
			}

			Host(string domain)
			{
				Domain = domain;
				Static = true;
			}

			public static Host MatchDomain(string domain)
			{
				// try static hosts
				Host h;
				if (Hosts.TryGetValue(domain, out h))
					return h;

				return Host.Any;
			}

			public bool MatchesDomain(string domain)
			{
				if (Static)
					return Domain == domain;
				else
					throw new NotImplementedException();
			}

			static Dictionary<string, Route> StaticRoutes = new Dictionary<string, Route>();
			static List<Route> RegexRoutes = new List<Route>();
			public Route MatchRoute(string path)
			{
				Route ret;
				if (StaticRoutes.TryGetValue(path, out ret))
					return ret;
				// try patterns

				return null;
			}

			public void Route(string path, PageGenerator page)
			{
				// assume static for now
				Route ret;
				if (StaticRoutes.TryGetValue(path, out ret))
					throw new ArgumentException($"{path} has already been statically routed");
				StaticRoutes[path] = new Route(path, page);
			}
		}
		
		
		public static async Task<bool> HandleRequest(params object[] args)
		{
			Request req = (Request)args[0];
			Response res = (Response)args[1];

			Host host = Host.MatchDomain(req.Host);
			Route route = host.MatchRoute(req.Path);

			if (route == null)
				throw new HttpException($"{req.Path} could not be found.", Status.NotFound);

			await route.Generator(req, res, new string[] { req.Path });
			return false;
		}
	}
}
