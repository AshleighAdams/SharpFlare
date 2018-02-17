using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using SharpFlare.Http;

using PageGenerator = System.Func<SharpFlare.Http.Request, SharpFlare.Http.Response, string[], System.Threading.Tasks.Task>;
using System.Text.RegularExpressions;

namespace SharpFlare
{
	[AttributeUsage(AttributeTargets.Method)]
	public class RouteAttribute : System.Attribute
	{
		public RouteAttribute(string path)
		{
			Path = path;
		}
		public string Path;
	}

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
			public readonly Regex Pattern;

			public Route(string path, PageGenerator gen)
			{
				Static = true;
				Path = path;
				Generator = gen;
			}
			public Route(Regex pattern, PageGenerator gen)
			{
				Static = false;
				Pattern = pattern;
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
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					Host h;
					if (Hosts.TryGetValue(domain, out h))
						return h;
					h = new Host(domain);
					Hosts[domain] = h;
					return h;
				}
			}

			Host(string domain)
			{
				Domain = domain;
				Static = true;
			}

			public static Host MatchDomain(string domain)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					// try static hosts
					Host h;
					if (Hosts.TryGetValue(domain, out h))
						return h;

					return Host.Any;
				}
			}

			public bool MatchesDomain(string domain)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					if (Static)
						return Domain == domain;
					else
						throw new NotImplementedException();
				}
			}

			static Dictionary<string, Route> StaticRoutes = new Dictionary<string, Route>();
			static List<Route> RegexRoutes = new List<Route>();
			public Route MatchRoute(string path, out string[] args)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					Route ret;
					if (StaticRoutes.TryGetValue(path, out ret))
					{
						args = new string[] { path };
						return ret;
					}
					// try patterns
					// todo: handle collisions
					foreach (Route r in RegexRoutes)
					{
						var m = r.Pattern.Match(path);
						if (!m.Success)
							continue;
						args = new string[m.Groups.Count];
						for (int i = 0; i < m.Groups.Count; i++)
							args[i] = m.Groups[i].Value;
						return r;
					}

					args = null;
					return null;
				}
			}

			public void Route(string path, PageGenerator page)
			{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
				{
					if (path.Contains('\\') || path.Contains('[') || path.Contains('('))
					{
						Regex rx = new Regex(path, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
						RegexRoutes.Add(new Route(rx, page));
					}
					else
					{
						Route ret;
						if (StaticRoutes.TryGetValue(path, out ret))
							throw new ArgumentException($"The route {path} has already been routed.");
						StaticRoutes[path] = new Route(path, page);
					}
				}
			}
		}

		public static async Task<bool> HandleRequest(params object[] args)
		{
#if SHARPFLARE_PROFILE
using (var _prof = SharpFlare.Profiler.EnterFunction())
#endif
			{
				Request req = (Request)args[0];
				Response res = (Response)args[1];

				string[] page_args;

				Host host = Host.MatchDomain(req.Url.Host);
				Route route = host.MatchRoute(req.Url.Path, out page_args);

				if (route == null)
					throw new HttpException($"{req.Url.Path} could not be found.", Status.NotFound);
#if SHARPFLARE_PROFILE
using (var _prof2 = SharpFlare.Profiler.EnterFunction("PageGenerator"))
#endif
				await route.Generator(req, res, page_args);
				await res.Finalize();

				return false;
			}
		}
	}
}
