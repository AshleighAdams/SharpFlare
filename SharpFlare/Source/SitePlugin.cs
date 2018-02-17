using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

using SharpFlare.Http;

#if NET_STANDARD
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Runtime.Remoting;
#endif

#if NET_STANDARD_NOPE

//https://msdn.microsoft.com/en-us/library/bb763046(v=vs.110).aspx
[assembly: SecurityRules(SecurityRuleSet.Level1)]
[assembly: AllowPartiallyTrustedCallers]

namespace SharpFlare
{
	public class BaseSitePlugin
	{
		string Name;
		public BaseSitePlugin(string name) { Name = name; }
		public virtual void Load() { }
		public virtual void Unload() { }
	}

	// this class is ran in the remote plugin and glues the host app and plugin together
	public class PluginHost : MarshalByRefObject
	{
		public PluginHost()
		{
		}

		public List<Tuple<string, string, MethodInfo>> Routes = new List<Tuple<string, string, MethodInfo>>();

		public Assembly Load(string path)
		{
			try
			{
				return Assembly.LoadFile(path);
			}
			catch (SecurityException ex)
			{
				(new PermissionSet(PermissionState.Unrestricted)).Assert();
				Console.WriteLine(ex.ToString());
				CodeAccessPermission.RevertAssert();
				throw;
			}
		}
	}

	//[Serializable]
	public class SitePlugin
	{
		string FullPath;
		string basepath;
		AppDomainSetup Setup;
		PermissionSet Permissions;
		AppDomain Sandbox;
		PluginHost Remote;
		object RemoteObject;

		[SecurityCritical/*(SecurityCriticalScope.Everything)*/]
		public SitePlugin(string path)
		{
			path = path.Replace('\\', '/');
			FullPath = path;
			string dir = Path.GetDirectoryName(FullPath).Replace('\\', '/');


			Setup = new AppDomainSetup()
			{
				ApplicationBase = dir,
				PrivateBinPath = dir,
				PrivateBinPathProbe = dir,
				ShadowCopyFiles = "true"
			};

			Permissions = new PermissionSet(PermissionState.None);
			Permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
			Permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, dir));
			Permissions.AddPermission(new DnsPermission(PermissionState.Unrestricted));
			Permissions.AddPermission(new UIPermission(PermissionState.Unrestricted));
			Permissions.AddPermission(new ReflectionPermission(PermissionState.Unrestricted));


			// class PluginRemote : MarshalByRefObject
			//System.Security.Policy.Evidence
			//StrongName pluginhost = typeof(PluginHost).Assembly.Evidence.GetHostEvidence<StrongName>();
			//AppDomain.MonitoringIsEnabled = true; // todo performance
			// var _x = Sandbox.MonitoringSurvivedMemorySize;// Sandbox.MonitoringTotalAllocatedMemorySize;
			// var _y = Sandbox.MonitoringTotalProcessorTime;
			StrongName[] list = new StrongName[0];
			Sandbox = AppDomain.CreateDomain(FullPath, null, Setup, Permissions, /*pluginhost*/ list);
			

			Sandbox.Load(Assembly.GetExecutingAssembly().FullName);
			//Sandbox.AssemblyResolve += Sandbox_AssemblyResolve;

			//Remote = (PluginHost)Sandbox.CreateInstanceFromAndUnwrap(typeof(PluginHost).Assembly.ManifestModule.FullyQualifiedName, typeof(PluginHost).FullName);

			ObjectHandle handle = Activator.CreateInstanceFrom(Sandbox, typeof(PluginHost).Assembly.ManifestModule.FullyQualifiedName, typeof(PluginHost).FullName);
			Remote = (PluginHost)handle.Unwrap();

			Assembly asm;
			{
				// copy ourselves to the dir path so they can load us:
				//string to = dir + "/" + Path.GetFileName(Assembly.GetExecutingAssembly().Location);
				
				try
				{
					//File.Copy(Assembly.GetExecutingAssembly().Location, to, true);
					//do
					//	System.Threading.Thread.Sleep(100);
					//while (!File.Exists(to));
					// FullPath.Replace("/", "\\")

					asm = Remote.Load(path);
					//asm = Remote.Load(FullPath.Replace("/", "\\"));
				}
				finally
				{
					//File.Delete(to);
				}
			}

			var name = Path.GetFileNameWithoutExtension(path).Substring("Site.".Length).Replace(".", " ").Replace(" ", "_");
			var t = asm.GetType(name);
			RemoteObject = Activator.CreateInstance(t);
			var methods =
				from m in t.GetMethods()
				where Attribute.IsDefined(m, typeof(RouteAttribute))
				select m;

			foreach (var method in methods)
			{
				MethodInfo mi = method;
				RouteAttribute attr = (RouteAttribute)method.GetCustomAttributes(typeof(RouteAttribute), false)[0];
				ParameterInfo[] args = method.GetParameters();

				if (method.ReturnType != typeof(Task) || args.Length != 3)
					throw new InvalidFunctionSignatureException($"Plugin route has invalid function signature: {t.Name}.{method.Name}");

				if (!CLI.GlobalOptions.DebugBindDelegate)
				{
					var param1 = Expression.Parameter(typeof(Request), "req");
					var param2 = Expression.Parameter(typeof(Response), "res");
					var param3 = Expression.Parameter(typeof(string[]), "args");

					Expression bound_exp = Expression.Call( // (Task)RemoteObject.method(Request, Response, string[])
						Expression.Constant(RemoteObject, t),
						method,
						param1, param2, param3
					);
					var bound = Expression.Lambda<Func<Request, Response, string[], Task>>(bound_exp, param1, param2, param3).Compile();

					Router.Host.Any.Route(attr.Path, bound);
				}
				else
				{
					Router.Host.Any.Route(attr.Path, async delegate (Request a0, Response a1, string[] a2)
					{
						await (Task)mi.Invoke(RemoteObject, new object[] { a0, a1, a2 });
					});
				}
			}
			
			

			//IPlugin plugin = (IPlugin)Sandbox.CreateInstanceAndUnwrap(typeof(IPlugin).Assembly.ManifestModule.FullyQualifiedName, typeof(IPlugin).FullName);
			//Activator.CreateInstanceFrom(Sandbox, typeof(IPlugin).Assembly.ManifestModule.FullyQualifiedName, typeof(IPlugin).FullName);


			//instance.ExecuteUntrustedCode(untrustedAssembly, untrustedClass, entryPoint, parameters);
			//Sandbox.UnhandledException
			//AssemblyResolve to load outside of ApplicationBase


			// foreach (var file in Directory.GetFiles(

			//Sandbox.Load(path);
			//Sandbox.
		}

		private Assembly Sandbox_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			string asmname = args.Name;
			if (args.Name.IndexOf(',') > 0)
				asmname = args.Name.Substring(0, args.Name.IndexOf(','));
			if (File.Exists(asmname))
				return Assembly.LoadFrom(basepath + asmname);
			if (File.Exists(basepath + asmname))
				return Assembly.LoadFrom(basepath + asmname);
			return Assembly.GetExecutingAssembly().FullName == args.Name ? Assembly.GetExecutingAssembly() : null;
		}
	}
}


#else
namespace SharpFlare
{
	public class SitePlugin
	{
		object RemoteObject;

		public SitePlugin(string path)
		{
			Assembly asm = Assembly.LoadFile(path);

			var name = Path.GetFileNameWithoutExtension(path).Substring("Site.".Length).Replace(".", " ").Replace(" ", "_");
			var t = asm.GetType(name);
			RemoteObject = Activator.CreateInstance(t);

			var methods =
				from m in t.GetMethods()
				where Attribute.IsDefined(m, typeof(RouteAttribute))
				select m;

			foreach (var method in methods)
			{
				MethodInfo mi = method;
				RouteAttribute attr = (RouteAttribute)method.GetCustomAttributes(typeof(RouteAttribute), false)[0];
				ParameterInfo[] args = method.GetParameters();

				if (method.ReturnType != typeof(Task) || args.Length != 3)
					throw new InvalidFunctionSignatureException($"SitePlugin route has invalid function signature: {t.Name}.{method.Name}");

				if (!CLI.GlobalOptions.DebugBindDelegate)
				{
					var param1 = Expression.Parameter(typeof(Request), "req");
					var param2 = Expression.Parameter(typeof(Response), "res");
					var param3 = Expression.Parameter(typeof(string[]), "args");

					Expression bound_exp = Expression.Call( // (Task)RemoteObject.method(Request, Response, string[])
						Expression.Constant(RemoteObject, t),
						method,
						param1, param2, param3
					);
					var bound = Expression.Lambda<Func<Request, Response, string[], Task>>(bound_exp, param1, param2, param3).Compile();

					Router.Host.Any.Route(attr.Path, bound);
				}
				else
				{
					Router.Host.Any.Route(attr.Path, async delegate (Request a0, Response a1, string[] a2)
					{
						await (Task)mi.Invoke(RemoteObject, new object[] { a0, a1, a2 });
					});
				}
			}
		}
	}
}
#endif
