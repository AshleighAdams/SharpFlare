using System;
using System.Net;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;

using System.Collections.Concurrent;

namespace SharpFlare
{
	public static class TaskPool
	{
		static ConcurrentQueue<Task> Tasks = new ConcurrentQueue<Task>();
		
		public static Task Run(Func<Task> act)
		{
			Task x = Task.Factory.StartNew(act,
				CancellationToken.None,

				TaskCreationOptions.LongRunning |
				TaskCreationOptions.DenyChildAttach |
				TaskCreationOptions.RunContinuationsAsynchronously,

				TaskScheduler.Default);
			x.ConfigureAwait(false);
			
			Tasks.Enqueue(x);
			return x;
		}

		public static void WaitAll()
		{
			while (!Tasks.IsEmpty)
			{
				if (Tasks.TryPeek(out Task top))
				{
					top.Wait();
					Tasks.TryDequeue(out top);
				}
				else
					Thread.Sleep(10);
			}
		}
	}
} 
