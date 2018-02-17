using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// ease the preasure on the garbage collector by avoiding using new objects where possible TODO: add shrink support
namespace SharpFlare
{
	public class ObjectPool<T>
	{
		// this uses a stack as more recent objects are more likley to be in the cpu cache
		//ThreadLocal<Stack<T>> ObjectsAllocated = new ThreadLocal<Stack<T>>(() =>
		//	{
		//		return new Stack<T>();
		//	});

		//ConcurrentDictionary<int, Stack<T>> Stacks = new ConcurrentDictionary<int, Stack<T>>();

		ConcurrentBag<T> ObjectsAllocated = new ConcurrentBag<T>();

		Func<T> Allocator;

		public ObjectPool(Func<T> allocator)
		{
			Allocator = allocator;
		}

		public T Take()
		{
			// /*
			T ret;
			if (ObjectsAllocated.TryTake(out ret))
				return ret;
			else
				return Allocator();
			// */

			//Stack<T> stack = ObjectsAllocated.Value;

			/*
			int thread = Thread.CurrentThread.ManagedThreadId;
			if (!Stacks.TryGetValue(thread, out stack))
				stack = Stacks[thread] = new Stack<T>();
			*/

			//if (stack.Count > 0)
			//	return stack.Pop();
			//else
			//	return Allocator();
		}

		// objects might not be returned to the same object pool, the task might switch over to a different thread, which is fine
		public void Return(T obj)
		{
			ObjectsAllocated.Add(obj);

			//Stack<T> stack = ObjectsAllocated.Value;
			//stack.Push(obj);

			/*
			Stack<T> stack;// = ObjectsAllocated.Value;
			int thread = Thread.CurrentThread.ManagedThreadId;
			if (!Stacks.TryGetValue(thread, out stack))
				stack = Stacks[thread] = new Stack<T>();
			stack.Push(obj);
			*/
		}
	}
}