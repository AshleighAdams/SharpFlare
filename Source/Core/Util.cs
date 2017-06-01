using System;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpFlare
{
	public static class Util
	{
		//                       0th   1st   2nd   3rd   4th   5th   6th   7th   8th   9th
		static string[] nths = { "th", "st", "nd", "rd", "th", "th", "th", "th", "th", "th" };
		public static string Nth(int n)
		{
			int mod100 = n % 100;

			if(mod100 >= 11 && mod100 <= 13)
				return nths[0];
			return nths[mod100 % 10];
		}

		public static string ToHttpDate(this DateTime when)
		{
			//  Sun, 06 Nov 1994 08:49:37 GMT
			DateTime utc = when.ToUniversalTime();
			return utc.ToString("ddd, dd MMM yyyy HH':'mm':'ss 'GMT'");
		}

		// gets a more traditional and readable stack trace
		static Regex asyncregex = new Regex("at (?<namespace>.*)\\.<(?<method>.*)>(?<bit>.*).MoveNext\\(\\) in (?<file>.*):line (?<line>[0-9]*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
		static Regex syncregex = new Regex("at (?<method>.*) in (?<file>.*):line (?<line>[0-9]*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

		[CLI.Option("Show the directory on public stack traces.", "debug-stack-show-directory")]
		public static bool StackShowDir = false;

		public static string SourceCodeBase = "";
		public static string CleanAsyncStackTrace(string stacktrace, bool ispublic = true)
		{
			string[] lines = stacktrace.Split('\n');
			StringBuilder sb = new StringBuilder();
			
			foreach (string _ in lines)
			{
				string line = _.Trim();

				if (line == "--- End of stack trace from previous location where exception was thrown ---")
					continue;
				if (line.Contains("System.Runtime.CompilerServices.TaskAwaiter"))
					continue;

				if (line.Contains(".MoveNext()"))
				{
					Match match = asyncregex.Match(line);
					if (match.Success)
					{
						string @namespace = match.Groups["namespace"].Value;
						string method = match.Groups["method"].Value;
						string file = match.Groups["file"].Value;
						string linenum = match.Groups["line"].Value;

						// sanatize the file
						file = file.Replace('\\', '/').Replace(Util.SourceCodeBase, "SharpFlare");
						line = $"{file}:{linenum} in async {@namespace}.{method}(...)";
						sb.AppendLine(line);
					}
				}
				else
				{
					Match match = syncregex.Match(line);

					if (match.Success)
					{
						string method = match.Groups["method"].Value;
						string file = match.Groups["file"].Value;
						string linenum = match.Groups["line"].Value;

						// sanatize the file
						file = file.Replace('\\', '/').Replace(Util.SourceCodeBase, "SharpFlare");

						line = $"{file}:{linenum} in {method}";
						sb.AppendLine(line);
					}
				}

				
			}

			return sb.ToString();
		}
	}
}

