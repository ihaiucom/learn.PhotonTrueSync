using System;
using System.Collections.Generic;

namespace ExitGames.Client.Photon
{
	internal class InvocationCache
	{
		private class CachedOperation
		{
			public int InvocationId
			{
				get;
				set;
			}

			public Action Action
			{
				get;
				set;
			}
		}

		private readonly LinkedList<InvocationCache.CachedOperation> cache = new LinkedList<InvocationCache.CachedOperation>();

		private int nextInvocationId = 1;

		public int NextInvocationId
		{
			get
			{
				return this.nextInvocationId;
			}
		}

		public int Count
		{
			get
			{
				return this.cache.Count;
			}
		}

		public void Reset()
		{
			LinkedList<InvocationCache.CachedOperation> obj = this.cache;
			lock (obj)
			{
				this.nextInvocationId = 1;
				this.cache.Clear();
			}
		}

		public void Invoke(int invocationId, Action action)
		{
			LinkedList<InvocationCache.CachedOperation> obj = this.cache;
			lock (obj)
			{
				bool flag = invocationId < this.nextInvocationId;
				if (!flag)
				{
					bool flag2 = invocationId == this.nextInvocationId;
					if (flag2)
					{
						this.nextInvocationId++;
						action();
						bool flag3 = this.cache.Count > 0;
						if (flag3)
						{
							LinkedListNode<InvocationCache.CachedOperation> linkedListNode = this.cache.First;
							while (linkedListNode != null && linkedListNode.Value.InvocationId == this.nextInvocationId)
							{
								this.nextInvocationId++;
								linkedListNode.Value.Action();
								linkedListNode = linkedListNode.Next;
								this.cache.RemoveFirst();
							}
						}
					}
					else
					{
						InvocationCache.CachedOperation value = new InvocationCache.CachedOperation
						{
							InvocationId = invocationId,
							Action = action
						};
						bool flag4 = this.cache.Count == 0;
						if (flag4)
						{
							this.cache.AddLast(value);
						}
						else
						{
							for (LinkedListNode<InvocationCache.CachedOperation> linkedListNode2 = this.cache.First; linkedListNode2 != null; linkedListNode2 = linkedListNode2.Next)
							{
								bool flag5 = linkedListNode2.Value.InvocationId > invocationId;
								if (flag5)
								{
									this.cache.AddBefore(linkedListNode2, value);
									return;
								}
							}
							this.cache.AddLast(value);
						}
					}
				}
			}
		}
	}
}
