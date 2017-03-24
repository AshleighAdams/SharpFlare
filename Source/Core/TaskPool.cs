using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SharpFlare
{
	public static class TaskPool
	{
		static LinkedList<Task> Tasks = new LinkedList<Task>();

		public static Task Run(Action act)
		{
			Task x = Task.Factory.StartNew(act,
				CancellationToken.None,

				TaskCreationOptions.LongRunning |
				TaskCreationOptions.DenyChildAttach |
				TaskCreationOptions.RunContinuationsAsynchronously,

				TaskScheduler.Default);
			//lock(Tasks)
			//{
			//	Tasks.AddLast(x);
			//}
			return x;
		}

		public static void WaitAll()
		{
			//Task t = null;
			//while(true)
			//{
			//	lock(Tasks)
			//	{
			//		if(t != null)
			//		{
			//			//Assert(Tasks.First == t);
			//			Tasks.RemoveFirst();
			//			t = null;
			//		}
			//		if(Tasks.Count == 0)
			//			break;

			//		t = Tasks.First.Value;
			//	}

			//	t.Wait();
			//}
		}
	}
} 
