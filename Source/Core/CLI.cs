using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using SharpFlare;
using SharpFlare.Logger;

namespace SharpFlare
{
	namespace CLI
	{
		[AttributeUsage(AttributeTargets.Field)]
		public class OptionAttribute : System.Attribute
		{
			public string Description;
			public string Long;
			public char Short;

			public OptionAttribute(string description, string longname, char shortname = '\0')
			{
				Description = description;
				Long = longname;
				Short = shortname;
			}
		}

		public static class Options
		{
			public static List<string> Arguments = new List<string>();
			public static bool Parse(string[] args)
			{
				Dictionary<string, string> opts = new Dictionary<string, string>();
				List<char> short_opts = new List<char>();

				Dictionary<string, bool> opts_okay = new Dictionary<string, bool>();
				Dictionary<char, bool> shrt_okay = new Dictionary<char, bool>();

				bool reading_options = true;

				foreach(string arg in args)
				{
					if(arg == "--")
					{
						reading_options = false;
						continue;
					}
					if(reading_options && arg.StartsWith("--"))
					{
						string key, val;
						if(arg.Contains("="))
						{
							int n = arg.IndexOf('=');
							key = arg.Substring(2, n - 2);
							val = arg.Substring(n + 1);
						}
						else
						{
							key = arg.Substring(2);
							if(opts.ContainsKey(key))
							{
								int count;
								if(int.TryParse(opts[key], out count))
									val = (++count).ToString();
								else
									val = opts[key];
							}
							else
								val = "1";
						}
						opts[key] = val;
					}
					else if(reading_options && arg.StartsWith("-"))
						foreach(char c in arg.Substring(1))
							short_opts.Add(c);
					else
						Arguments.Add(arg);
				}

				// it's parsed, let's update attributes
				var fields =
					from a in AppDomain.CurrentDomain.GetAssemblies()
					from t in a.GetTypes()
					from f in t.GetFields()
					where Attribute.IsDefined(f, typeof(OptionAttribute))
					select f;

				foreach(var field in fields)
				{
					OptionAttribute attr = (OptionAttribute)field.GetCustomAttributes(typeof(OptionAttribute), false)[0];

					// let's mark this one as okay:
					opts_okay[attr.Long] = true;
					shrt_okay[attr.Short] = true;

					string val = "0";
					int lo_count;
					int so_count = 0;

					bool has_long = opts.TryGetValue(attr.Long, out val);
					foreach(char c in short_opts)
						if(c == attr.Short)
							so_count++;

					if(so_count == 0 && !has_long) // isn't set
						continue;
					if( has_long && int.TryParse(val, out lo_count) && so_count > 0)
						val = (lo_count + so_count).ToString();
					else if (!has_long)
						val = so_count.ToString();

					switch(Type.GetTypeCode(field.FieldType))
					{
					case TypeCode.String:
						field.SetValue(null, val);
						break;
					case TypeCode.Boolean:
						field.SetValue(null, val != "0");
						break;
					case TypeCode.Double:
						{
							double pval;
							if(double.TryParse(val, out pval))
								field.SetValue(null, pval);
						} break;
					case TypeCode.Int64:
						{
							Int64 pval;
							if(Int64.TryParse(val, out pval))
								field.SetValue(null, pval);
						} break;
					case TypeCode.UInt64:
						{
							UInt64 pval;
							if(UInt64.TryParse(val, out pval))
								field.SetValue(null, pval);
						} break;
					default:
						break;
					}
				}

				bool okay = true;

				foreach(KeyValuePair<string, string> pair in opts)
					if(!opts_okay.ContainsKey(pair.Key))
					{
						GlobalLogger.Message(Level.Warning, "unknown command line option: --{0}", pair.Key);
						okay = false;
					}

				foreach(char c in short_opts)
					if(!shrt_okay.ContainsKey(c))
					{
						GlobalLogger.Message(Level.Warning, "unknown command line flag: -{0}", c);
						okay = false;
					}

				return okay;
			}
		}
	}
}
