using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

using SharpFlare.Logger;

namespace SharpFlare
{
	namespace Hooks
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
			public Func<object[], bool> Function;

			public HookInfo(string name, string id, double order, Func<object[], bool> func)
			{
				Name = name;
				Id = id;
				Order = order;
				Function = func;
			}
		}

		public static class Hook
		{

			static Dictionary<string, Dictionary<string, HookInfo>> table = new Dictionary<string, Dictionary<string, HookInfo>>();

			public static HookInfo Add(string name, string id, Func<object[], bool> func, double order = 0)
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
			
			public static bool Call(string name, params object[] args)
			{
				Dictionary<string, HookInfo> hookinfos;
				if(!table.TryGetValue(name, out hookinfos))
					return false;
				
				foreach(var pair in hookinfos)
				{
					HookInfo info = pair.Value;
					if(info.Function(args))
						return true;
				}
				return false;
			}
		}

		public class InvalidHookSignatureException : Exception
		{
			public InvalidHookSignatureException() {}
			public InvalidHookSignatureException(string message) : base(message) {}
			public InvalidHookSignatureException(string message, Exception inner) : base(message, inner) {}
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

					if(method.ReturnType != typeof(Boolean) || args.Length != 1)
					{
						GlobalLogger.Message(Level.Critical, "hook has invalid type signature: {0}.{1}", t.Name, method.Name);
						throw new InvalidHookSignatureException($"hook has invalid type signature: {t.Name}.{method.Name}");
					}

					string InstanceId = (++_HookCurrentInstance).ToString();
					string id = $"{method.Name}@{InstanceId}";

					GlobalLogger.Message(Level.Verbose, $"new hook: {t.Name}.{method.Name}; hook: {attr.Name} id: {id}, order: {attr.Order}");
					
					object me = (object)this;
					Func<object[], bool> curry =
						delegate(object[] meargs)
						{
							return (bool)method.Invoke(me, new object[] { meargs });
						};
						
					HookInfo hook = SharpFlare.Hooks.Hook.Add(attr.Name, id, curry, attr.Order);
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
					SharpFlare.Hooks.Hook.Remove(hook.Name, hook.Id);
				}
				
				this.Hooks.Clear();
				
				this.Hooks.Clear();
				this.Hooks = null;
				this.Hooked = false;
			}
		}
	}
}

