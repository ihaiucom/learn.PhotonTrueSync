using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace ExitGames.Client.Photon
{
	public class SupportClass
	{
		public delegate int IntegerMillisecondsDelegate();

		public class ThreadSafeRandom
		{
			private static readonly Random _r = new Random();

			public static int Next()
			{
				Random r = SupportClass.ThreadSafeRandom._r;
				int result;
				lock (r)
				{
					result = SupportClass.ThreadSafeRandom._r.Next();
				}
				return result;
			}
		}

		[CompilerGenerated]
		[Serializable]
		private sealed class <>c
		{
			public static readonly SupportClass.<>c <>9 = new SupportClass.<>c();

			internal int cctor>b__15_0()
			{
				return Environment.TickCount;
			}
		}

		protected internal static SupportClass.IntegerMillisecondsDelegate IntegerMilliseconds = new SupportClass.IntegerMillisecondsDelegate(SupportClass.<>c.<>9.<.cctor>b__15_0);

		public static uint CalculateCrc(byte[] buffer, int length)
		{
			uint num = 4294967295u;
			uint num2 = 3988292384u;
			for (int i = 0; i < length; i++)
			{
				byte b = buffer[i];
				num ^= (uint)b;
				for (int j = 0; j < 8; j++)
				{
					bool flag = (num & 1u) > 0u;
					if (flag)
					{
						num = (num >> 1 ^ num2);
					}
					else
					{
						num >>= 1;
					}
				}
			}
			return num;
		}

		public static List<MethodInfo> GetMethods(Type type, Type attribute)
		{
			List<MethodInfo> list = new List<MethodInfo>();
			bool flag = type == null;
			List<MethodInfo> result;
			if (flag)
			{
				result = list;
			}
			else
			{
				MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				MethodInfo[] array = methods;
				for (int i = 0; i < array.Length; i++)
				{
					MethodInfo methodInfo = array[i];
					bool flag2 = attribute == null || methodInfo.IsDefined(attribute, false);
					if (flag2)
					{
						list.Add(methodInfo);
					}
				}
				result = list;
			}
			return result;
		}

		public static int GetTickCount()
		{
			return SupportClass.IntegerMilliseconds();
		}

		public static void CallInBackground(Func<bool> myThread)
		{
			SupportClass.CallInBackground(myThread, 100);
		}

		public static void CallInBackground(Func<bool> myThread, int millisecondsInterval)
		{
			new Thread(delegate
			{
				while (myThread())
				{
					Thread.Sleep(millisecondsInterval);
				}
			})
			{
				IsBackground = true
			}.Start();
		}

		public static void WriteStackTrace(Exception throwable, TextWriter stream)
		{
			bool flag = stream != null;
			if (flag)
			{
				stream.WriteLine(throwable.ToString());
				stream.WriteLine(throwable.StackTrace);
				stream.Flush();
			}
			else
			{
				Debug.WriteLine(throwable.ToString());
				Debug.WriteLine(throwable.StackTrace);
			}
		}

		public static void WriteStackTrace(Exception throwable)
		{
			SupportClass.WriteStackTrace(throwable, null);
		}

		public static string DictionaryToString(IDictionary dictionary)
		{
			return SupportClass.DictionaryToString(dictionary, true);
		}

		public static string DictionaryToString(IDictionary dictionary, bool includeTypes)
		{
			bool flag = dictionary == null;
			string result;
			if (flag)
			{
				result = "null";
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("{");
				foreach (object current in dictionary.Keys)
				{
					bool flag2 = stringBuilder.Length > 1;
					if (flag2)
					{
						stringBuilder.Append(", ");
					}
					bool flag3 = dictionary[current] == null;
					Type type;
					string text;
					if (flag3)
					{
						type = typeof(object);
						text = "null";
					}
					else
					{
						type = dictionary[current].GetType();
						text = dictionary[current].ToString();
					}
					bool flag4 = typeof(IDictionary) == type || typeof(Hashtable) == type;
					if (flag4)
					{
						text = SupportClass.DictionaryToString((IDictionary)dictionary[current]);
					}
					bool flag5 = typeof(string[]) == type;
					if (flag5)
					{
						text = string.Format("{{{0}}}", string.Join(",", (string[])dictionary[current]));
					}
					bool flag6 = typeof(byte[]) == type;
					if (flag6)
					{
						text = string.Format("byte[{0}]", ((byte[])dictionary[current]).Length);
					}
					if (includeTypes)
					{
						stringBuilder.AppendFormat("({0}){1}=({2}){3}", new object[]
						{
							current.GetType().Name,
							current,
							type.Name,
							text
						});
					}
					else
					{
						stringBuilder.AppendFormat("{0}={1}", current, text);
					}
				}
				stringBuilder.Append("}");
				result = stringBuilder.ToString();
			}
			return result;
		}

		[Obsolete("Use DictionaryToString() instead.")]
		public static string HashtableToString(Hashtable hash)
		{
			return SupportClass.DictionaryToString(hash);
		}

		public static string ByteArrayToString(byte[] list)
		{
			bool flag = list == null;
			string result;
			if (flag)
			{
				result = string.Empty;
			}
			else
			{
				result = BitConverter.ToString(list);
			}
			return result;
		}
	}
}
