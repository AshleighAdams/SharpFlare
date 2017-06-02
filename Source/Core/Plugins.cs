using System;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Runtime.Remoting;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SharpFlare.Http;
using System.Collections.Generic;
using System.Linq.Expressions;

//https://msdn.microsoft.com/en-us/library/bb763046(v=vs.110).aspx
//[assembly: SecurityRules(SecurityRuleSet.Level1)]
[assembly: AllowPartiallyTrustedCallers]

namespace SharpFlare
{
	public class BasePlugin
	{
		string Name;
		public BasePlugin(string name) { Name = name; }
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
			return Assembly.LoadFile(path);
			/*
			try
			{
			}
			catch (SecurityException ex)
			{
				(new PermissionSet(PermissionState.Unrestricted)).Assert();
				Console.WriteLine(ex.ToString());
				CodeAccessPermission.RevertAssert();
			}*/
		}
	}

	public class Plugin
	{
		string FullPath;
		string basepath;
		AppDomainSetup Setup;
		PermissionSet Permissions;
		AppDomain Sandbox;
		PluginHost Remote;
		object RemoteObject;

		[SecurityCritical/*(SecurityCriticalScope.Everything)*/]
		public Plugin(string path)
		{
			basepath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			AppDomain.MonitoringIsEnabled = true; // todo performance

			FullPath = Path.GetFullPath(path);
			string dir = Path.GetDirectoryName(FullPath);

			Setup = new AppDomainSetup()
			{
				ApplicationBase = dir,
				PrivateBinPath = dir,
				PrivateBinPathProbe = dir
			};

			Permissions = new PermissionSet(PermissionState.None);
			Permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
			Permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, dir));
			Permissions.AddPermission(new DnsPermission(PermissionState.Unrestricted));
			Permissions.AddPermission(new UIPermission(PermissionState.Unrestricted));
			Permissions.AddPermission(new ReflectionPermission(PermissionState.Unrestricted));


			// class PluginRemote : MarshalByRefObject  
			StrongName pluginhost = typeof(PluginHost).Assembly.Evidence.GetHostEvidence<StrongName>();

			Sandbox = AppDomain.CreateDomain(FullPath, null, Setup, Permissions, pluginhost);

			//Sandbox.AssemblyResolve += Sandbox_AssemblyResolve;

			//Remote = (PluginHost)Sandbox.CreateInstanceFromAndUnwrap(typeof(PluginHost).Assembly.ManifestModule.FullyQualifiedName, typeof(PluginHost).FullName);

			ObjectHandle handle = Activator.CreateInstanceFrom(Sandbox, typeof(PluginHost).Assembly.ManifestModule.FullyQualifiedName, typeof(PluginHost).FullName);
			Remote = (PluginHost)handle.Unwrap();
			Assembly asm = Remote.Load(FullPath);

			var t = asm.GetType("TestPlugin");
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
			
			var _x = Sandbox.MonitoringSurvivedMemorySize;// Sandbox.MonitoringTotalAllocatedMemorySize;
			var _y = Sandbox.MonitoringTotalProcessorTime;

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
