using System;
using System.Collections;
using System.Collections.Generic;

namespace ExitGames.Client.Photon
{
	public class Hashtable : Dictionary<object, object>
	{
		public new object this[object key]
		{
			get
			{
				object result = null;
				base.TryGetValue(key, out result);
				return result;
			}
			set
			{
				base[key] = value;
			}
		}

		public Hashtable()
		{
		}

		public Hashtable(int x) : base(x)
		{
		}

		public new IEnumerator<DictionaryEntry> GetEnumerator()
		{
			return new DictionaryEntryEnumerator(((IDictionary)this).GetEnumerator());
		}

		public override string ToString()
		{
			List<string> list = new List<string>();
			foreach (object current in base.Keys)
			{
				bool flag = current == null || this[current] == null;
				if (flag)
				{
					list.Add(current + "=" + this[current]);
				}
				else
				{
					list.Add(string.Concat(new object[]
					{
						"(",
						current.GetType(),
						")",
						current,
						"=(",
						this[current].GetType(),
						")",
						this[current]
					}));
				}
			}
			return string.Join(", ", list.ToArray());
		}

		public object Clone()
		{
			return new Dictionary<object, object>(this);
		}
	}
}
