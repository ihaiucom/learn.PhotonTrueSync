using System;
using System.Collections.Generic;

namespace ExitGames.Client.Photon
{
	public class Protocol
	{
		public static readonly IProtocol GpBinaryV16 = new Protocol16();

		public static readonly IProtocol GpBinaryV18;

		public static readonly IProtocol ProtocolDefault = Protocol.GpBinaryV16;

		internal static readonly Dictionary<Type, CustomType> TypeDict = new Dictionary<Type, CustomType>();

		internal static readonly Dictionary<byte, CustomType> CodeDict = new Dictionary<byte, CustomType>();

		private static readonly float[] memFloatBlock = new float[1];

		private static readonly byte[] memDeserialize = new byte[4];

		public static bool TryRegisterType(Type type, byte typeCode, SerializeMethod serializeFunction, DeserializeMethod deserializeFunction)
		{
			bool flag = Protocol.CodeDict.ContainsKey(typeCode) || Protocol.TypeDict.ContainsKey(type);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				CustomType value = new CustomType(type, typeCode, serializeFunction, deserializeFunction);
				Protocol.CodeDict.Add(typeCode, value);
				Protocol.TypeDict.Add(type, value);
				result = true;
			}
			return result;
		}

		public static bool TryRegisterType(Type type, byte typeCode, SerializeStreamMethod serializeFunction, DeserializeStreamMethod deserializeFunction)
		{
			bool flag = Protocol.CodeDict.ContainsKey(typeCode) || Protocol.TypeDict.ContainsKey(type);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				CustomType value = new CustomType(type, typeCode, serializeFunction, deserializeFunction);
				Protocol.CodeDict.Add(typeCode, value);
				Protocol.TypeDict.Add(type, value);
				result = true;
			}
			return result;
		}

		public static byte[] Serialize(object obj)
		{
			return Protocol.ProtocolDefault.Serialize(obj);
		}

		public static object Deserialize(byte[] serializedData)
		{
			return Protocol.ProtocolDefault.Deserialize(serializedData);
		}

		public static void Serialize(short value, byte[] target, ref int targetOffset)
		{
			int num = targetOffset;
			targetOffset = num + 1;
			target[num] = (byte)(value >> 8);
			num = targetOffset;
			targetOffset = num + 1;
			target[num] = (byte)value;
		}

		public static void Serialize(int value, byte[] target, ref int targetOffset)
		{
			int num = targetOffset;
			targetOffset = num + 1;
			target[num] = (byte)(value >> 24);
			num = targetOffset;
			targetOffset = num + 1;
			target[num] = (byte)(value >> 16);
			num = targetOffset;
			targetOffset = num + 1;
			target[num] = (byte)(value >> 8);
			num = targetOffset;
			targetOffset = num + 1;
			target[num] = (byte)value;
		}

		public static void Serialize(float value, byte[] target, ref int targetOffset)
		{
			float[] obj = Protocol.memFloatBlock;
			lock (obj)
			{
				Protocol.memFloatBlock[0] = value;
				Buffer.BlockCopy(Protocol.memFloatBlock, 0, target, targetOffset, 4);
			}
			bool isLittleEndian = BitConverter.IsLittleEndian;
			if (isLittleEndian)
			{
				byte b = target[targetOffset];
				byte b2 = target[targetOffset + 1];
				target[targetOffset + 0] = target[targetOffset + 3];
				target[targetOffset + 1] = target[targetOffset + 2];
				target[targetOffset + 2] = b2;
				target[targetOffset + 3] = b;
			}
			targetOffset += 4;
		}

		public static void Deserialize(out int value, byte[] source, ref int offset)
		{
			int num = offset;
			offset = num + 1;
			int arg_1E_0 = (int)source[num] << 24;
			num = offset;
			offset = num + 1;
			int arg_2C_0 = arg_1E_0 | (int)source[num] << 16;
			num = offset;
			offset = num + 1;
			int arg_38_0 = arg_2C_0 | (int)source[num] << 8;
			num = offset;
			offset = num + 1;
			value = (arg_38_0 | (int)source[num]);
		}

		public static void Deserialize(out short value, byte[] source, ref int offset)
		{
			int num = offset;
			offset = num + 1;
			byte arg_1A_0 = (byte)(source[num] << 8);
			num = offset;
			offset = num + 1;
			value = (short)(arg_1A_0 | source[num]);
		}

		public static void Deserialize(out float value, byte[] source, ref int offset)
		{
			bool isLittleEndian = BitConverter.IsLittleEndian;
			if (isLittleEndian)
			{
				byte[] obj = Protocol.memDeserialize;
				lock (obj)
				{
					byte[] array = Protocol.memDeserialize;
					byte[] arg_2C_0 = array;
					int arg_2C_1 = 3;
					int num = offset;
					offset = num + 1;
					arg_2C_0[arg_2C_1] = source[num];
					byte[] arg_3A_0 = array;
					int arg_3A_1 = 2;
					num = offset;
					offset = num + 1;
					arg_3A_0[arg_3A_1] = source[num];
					byte[] arg_48_0 = array;
					int arg_48_1 = 1;
					num = offset;
					offset = num + 1;
					arg_48_0[arg_48_1] = source[num];
					byte[] arg_56_0 = array;
					int arg_56_1 = 0;
					num = offset;
					offset = num + 1;
					arg_56_0[arg_56_1] = source[num];
					value = BitConverter.ToSingle(array, 0);
				}
			}
			else
			{
				value = BitConverter.ToSingle(source, offset);
				offset += 4;
			}
		}
	}
}
