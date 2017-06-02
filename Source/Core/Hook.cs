using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

using SharpFlare.Logger;
using System.Threading.Tasks;
using System.Linq.Expressions;

namespace SharpFlare
{
	/*
	static class HookTests : UnitTest
	{
		static bool Test()
		{
		}
	}
	*/
	[AttributeUsage(AttributeTargets.Method)]
	public class HookAttribute
		: System.Attribute
	{
		public HookAttribute(string name, double order = 0)
		{
			Name = name;
			Order = order;
		}

		public string Name;
		public string Id;
		public double Order;
	}

	public class HookInfo
	{
		public string Name;
		public string Id;
		public double Order;
		public Func<object[], Task<bool>> Function;

		public HookInfo(string name, string id, double order, Func<object[], Task<bool>> func)
		{
			Name = name;
			Id = id;
			Order = order;
			Function = func;
		}
	}
		
	public static class Hooks
	{
		static Dictionary<string, Dictionary<string, HookInfo>> table = new Dictionary<string, Dictionary<string, HookInfo>>();

		public static HookInfo Add(string name, string id, Func<object[], Task<bool>> func, double order = 0)
		{
			Dictionary<string, HookInfo> hookinfos;
			if(!table.TryGetValue(name, out hookinfos))
				table[name] = hookinfos = new Dictionary<string, HookInfo>();

			return hookinfos[id] = new HookInfo(name, id, order, func);
		}
			
		public static void Remove(string name, string id)
		{
			Dictionary<string, HookInfo> hookinfos;
			if(!table.TryGetValue(name, out hookinfos))
				return; // no hook by this name, already removed
			hookinfos.Remove(id);
		}
			
		public static async Task<bool> Call(string name, params object[] args)
		{
			Dictionary<string, HookInfo> hookinfos;
			if(!table.TryGetValue(name, out hookinfos))
				return false;
				
			foreach(var pair in hookinfos)
			{
				HookInfo info = pair.Value;
				if(await info.Function(args))
					return true;
			}
			return false;
		}
	}

	public class InvalidFunctionSignatureException : Exception
	{
		public InvalidFunctionSignatureException() {}
		public InvalidFunctionSignatureException(string message) : base(message) {}
		public InvalidFunctionSignatureException(string message, Exception inner) : base(message, inner) {}
	}

	public class Hookable
	{
		static BigInteger _HookCurrentInstance = new BigInteger();
		public bool Hooked = false;
		public LinkedList<HookInfo> Hooks;
			
		public void Hook()
		{
			if(this.Hooked)
				return;
			this.Hooks = new LinkedList<HookInfo>();
				
			Type t = this.GetType();
			var methods =
				from m in t.GetMethods()
				where Attribute.IsDefined(m, typeof(HookAttribute))
				select m;

			foreach(var method in methods)
			{
				HookAttribute attr = (HookAttribute)method.GetCustomAttributes(typeof(HookAttribute), false)[0];
				ParameterInfo[] args = method.GetParameters();

				if(method.ReturnType != typeof(Task<bool>) || args.Length != 1)
				{
					GlobalLogger.Message(Level.Critical, "hook has invalid type signature: {0}.{1}", t.Name, method.Name);
					throw new InvalidFunctionSignatureException($"hook has invalid type signature: {t.Name}.{method.Name}");
				}

				string InstanceId = (++_HookCurrentInstance).ToString();
				string id = $"{method.Name}@{InstanceId}";
				
				HookInfo hook;

				if (!CLI.GlobalOptions.DebugBindDelegate)
				{
					var param1 = Expression.Parameter(typeof(object[]), "args");
					Expression curry_exp = Expression.Call( // Task<bool>this.method(object[] args)
						Expression.Constant(this, this.GetType()),
						method,
						param1
					);
					var curry = Expression.Lambda<Func<object[], Task<bool>>>(curry_exp, param1).Compile();
					hook = SharpFlare.Hooks.Add(attr.Name, id, curry, attr.Order);
				}
				else
				{
					object me = (object)this;
					Func<object[], Task<bool>> curry =
						async delegate(object[] meargs)
						{
							Task<bool> res = (Task<bool>)method.Invoke(me, new object[] { meargs });
							return await res;
						};
					hook = SharpFlare.Hooks.Add(attr.Name, id, curry, attr.Order);
				}

				GlobalLogger.Message(Level.Verbose, $"new hook: {t.Name}.{method.Name}; hook: {attr.Name} id: {id}, order: {attr.Order}");
				Hooks.AddLast(hook);
			}
				
			this.Hooked = true;
		}
			
		public void Unhook()
		{
			if(!this.Hooked)
				return;
				
			foreach(var hook in this.Hooks)
			{
				GlobalLogger.Message(Level.Verbose, $"remove hook: {hook.Name}, {hook.Id}");
				SharpFlare.Hooks.Remove(hook.Name, hook.Id);
			}
				
			this.Hooks.Clear();
				
			this.Hooks.Clear();
			this.Hooks = null;
			this.Hooked = false;
		}
	}
}

