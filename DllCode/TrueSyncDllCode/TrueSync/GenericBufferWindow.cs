using System;

namespace TrueSync
{
	public class GenericBufferWindow<T>
	{
		public delegate T NewInstance();

		public T[] buffer;

		public int size;

		public int currentIndex;

		public GenericBufferWindow(int size)
		{
			this.size = size;
			this.currentIndex = 0;
			this.buffer = new T[size];
			for (int i = 0; i < size; i++)
			{
				this.buffer[i] = Activator.CreateInstance<T>();
			}
		}

		public GenericBufferWindow(int size, GenericBufferWindow<T>.NewInstance NewInstance)
		{
			this.size = size;
			this.currentIndex = 0;
			this.buffer = new T[size];
			for (int i = 0; i < size; i++)
			{
				this.buffer[i] = NewInstance();
			}
		}

		public void Resize(int newSize)
		{
			bool flag = newSize == this.size;
			if (!flag)
			{
				T[] array = new T[newSize];
				int num = newSize - this.size;
				bool flag2 = newSize > this.size;
				if (flag2)
				{
					for (int i = 0; i < this.size; i++)
					{
						bool flag3 = i < this.currentIndex;
						if (flag3)
						{
							array[i] = this.buffer[i];
						}
						else
						{
							array[i + num] = this.buffer[i];
						}
					}
					for (int j = 0; j < num; j++)
					{
						array[this.currentIndex + j] = Activator.CreateInstance<T>();
					}
				}
				else
				{
					for (int k = 0; k < newSize; k++)
					{
						bool flag4 = k < this.currentIndex;
						if (flag4)
						{
							array[k] = this.buffer[k];
						}
						else
						{
							array[k] = this.buffer[k - num];
						}
					}
					this.currentIndex %= newSize;
				}
				this.buffer = array;
				this.size = newSize;
			}
		}

		public void Set(T instance)
		{
			this.buffer[this.currentIndex] = instance;
		}

		public T Previous()
		{
			int num = this.currentIndex - 1;
			bool flag = num < 0;
			if (flag)
			{
				num = this.size - 1;
			}
			return this.buffer[num];
		}

		public T Current()
		{
			return this.buffer[this.currentIndex];
		}

		public void MoveNext()
		{
			this.currentIndex = (this.currentIndex + 1) % this.size;
		}
	}
}
