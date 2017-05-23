using System;
using System.IO;

namespace SharpFlare
{
	namespace Logger
	{
		public enum Level
		{
			/* lvl       systemd      explanation */
			Verbatim,  // debug     word for word
			Verbose,   // debug     almost word for word
			Debug,     // debug     debug info
			Normal,    // info      default level (information)
			Notice,    // notice    info that is non-trivial
			Warning,   // warning   possible future errors
			Alert,     // alert     really bad warning
			Error,     // err       went bad, but subsystem recovered and still works
			Critical,  // crit      error + emphasis on us doing wrong
			Fatal      // emerg     unrecoverable, exits the program
		}

		// the default sink if a message was promoted all the way up (to console)
		public class Sink
		{
			public Sink()
			{
			}

			private static TextWriter StdOut = Console.Out;
			private static TextWriter StdErr = Console.Error;

			[CLI.Option("-VV: verbose; -VVV: verbose verbose (verbatim)", "verbose", 'V')]
			public static Int64 Verbosity = 0;

			public void Consume(Level lvl, string msg)
			{
				switch(lvl)
				{
				case Level.Debug:
				case Level.Warning:
				case Level.Alert:
				case Level.Error:
				case Level.Critical:
				case Level.Fatal:
					StdErr.WriteLine(msg);
					break;
				default:
					StdOut.WriteLine(msg);
					break;
				}
			}
		}


		public static class GlobalLogger
		{
			private static Sink sink = new Sink();
			public static void Message(Level lvl, string msg)
			{
				sink.Consume(lvl, msg);
			}
			public static void Message(Level lvl, string fmt, params object[] args)
			{
				sink.Consume(lvl, string.Format(fmt, args));
			}
		}
	}
}
