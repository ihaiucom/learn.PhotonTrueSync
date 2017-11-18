using System;
using System.Collections;
using System.Collections.Generic;

namespace ExitGames.Client.Photon
{
	public class DictionaryEntryEnumerator : IEnumerator<DictionaryEntry>, IEnumerator, IDisposable
	{
		private IDictionaryEnumerator enumerator;

		object IEnumerator.Current
		{
			get
			{
				return (DictionaryEntry)this.enumerator.Current;
			}
		}

		public DictionaryEntry Current
		{
			get
			{
				return (DictionaryEntry)this.enumerator.Current;
			}
		}

		public object Key
		{
			get
			{
				return this.enumerator.Key;
			}
		}

		public object Value
		{
			get
			{
				return this.enumerator.Value;
			}
		}

		public DictionaryEntry Entry
		{
			get
			{
				return this.enumerator.Entry;
			}
		}

		public DictionaryEntryEnumerator(IDictionaryEnumerator original)
		{
			this.enumerator = original;
		}

		public bool MoveNext()
		{
			return this.enumerator.MoveNext();
		}

		public void Reset()
		{
			this.enumerator.Reset();
		}

		public void Dispose()
		{
			this.enumerator = null;
		}
	}
}
