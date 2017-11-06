using System;
using System.Collections.Generic;

namespace TrueSync
{
	public abstract class ResourcePool
	{
		protected bool fresh = true;

		protected static List<ResourcePool> resourcePoolReferences = new List<ResourcePool>();

		public static void CleanUpAll()
		{
			int i = 0;
			int count = ResourcePool.resourcePoolReferences.Count;
			while (i < count)
			{
				ResourcePool.resourcePoolReferences[i].ResetResourcePool();
				i++;
			}
			ResourcePool.resourcePoolReferences.Clear();
		}

		public abstract void ResetResourcePool();
	}
	public class ResourcePool<T> : ResourcePool
	{
		protected Stack<T> stack = new Stack<T>(10);

		public int Count
		{
			get
			{
				return this.stack.Count;
			}
		}

		public override void ResetResourcePool()
		{
			this.stack.Clear();
			this.fresh = true;
		}

		public void GiveBack(T obj)
		{
			this.stack.Push(obj);
		}

		public T GetNew()
		{
			bool fresh = this.fresh;
			if (fresh)
			{
				ResourcePool.resourcePoolReferences.Add(this);
				this.fresh = false;
			}
			bool flag = this.stack.Count == 0;
			if (flag)
			{
				this.stack.Push(this.NewInstance());
			}
			T t = this.stack.Pop();
			bool flag2 = t is ResourcePoolItem;
			if (flag2)
			{
				((ResourcePoolItem)((object)t)).CleanUp();
			}
			return t;
		}

		protected virtual T NewInstance()
		{
			return Activator.CreateInstance<T>();
		}
	}
}
