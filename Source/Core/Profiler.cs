using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpFlare
{
	public static class Profiler
	{
		public class FuncTracker : IDisposable
		{
			string info;

			public FuncTracker(string info)
			{
				this.info = info;
			}

			~FuncTracker()
			{
				Dispose(false);
			}
			
			private bool disposed;
			private void Dispose(bool disposing)
			{
				if (!this.disposed)
				{
					this.disposed = true;
					if (disposing)
					{
						LeaveFunction(info);
					}
				}
			}
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		// time mem (+/-/~) funcname where:line args
		static bool running = false;
		public static bool CollectGarbage = true;
		static Stopwatch timer;
		static StreamWriter log;
		static FuncTracker dummy = new FuncTracker("");

		public static void Start(string logname)
		{
			if (running)
				throw new InvalidOperationException("a profiler is already running");
			Stop(); // cache this
			running = true;
			log = new StreamWriter(logname, false, new UTF8Encoding(false));
			timer = new Stopwatch();

			GC.Collect(3, GCCollectionMode.Forced, true, false);
			long kb = GC.GetTotalMemory(false) / 1000;
			string callerinfo = $" \t :0";
			log.Write($"{timer.Elapsed.TotalSeconds}\t{kb}\t+\t{callerinfo}\t\n");

			timer.Start();
		}

		public static void Stop()
		{
			if (!running)
				return;
			running = false;
			timer.Stop();

			if(CollectGarbage)
				GC.Collect(3, GCCollectionMode.Forced, true, false);
			double kb = (double)GC.GetTotalMemory(false) / 1000.0;
			string callerinfo = $" \t :0";
			log.Write($"{timer.Elapsed.TotalSeconds}\t{kb}\t-\t{callerinfo}\t\n");

			log.Close();
			log = null;
		}

		[MethodImplAttribute(MethodImplOptions.NoInlining)] 
		public static FuncTracker EnterFunction(
			[CallerMemberName] string caller = "unknown",
			[CallerFilePath]   string file   = "unknown",
			[CallerLineNumber] int    line   = -1,
			object[] args = null)
		{
			if (!running)
				return dummy;

			FuncTracker ret;
			timer.Stop();
			{
				if(CollectGarbage)
					GC.Collect(3, GCCollectionMode.Forced, true, false);
				double kb = (double)GC.GetTotalMemory(false) / 1000.0;

				string strargs = "";
				if (args != null)
				{
					foreach (object o in args)
						strargs += o.ToString() + ", ";
					args = null;
				}
				string callerinfo = $"{caller}\t{file}:{line}";
				log.Write($"{timer.Elapsed.TotalSeconds}\t{kb}\t+\t{callerinfo}\t{strargs}\n");
				ret = new FuncTracker(callerinfo);
			}
			timer.Start();

			return ret;
		}
		public static void LeaveFunction(string callerinfo)
		{
			if (!running)
				return;
			timer.Stop();
			{
				double kb = (double)GC.GetTotalMemory(false) / 1000.0;
				log.Write($"{timer.Elapsed.TotalSeconds}\t{kb}\t-\t{callerinfo}\t\n");
			}
			timer.Start();
		}
	}
}