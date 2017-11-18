using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ExitGames.Client.Photon
{
	public class Protocol16 : IProtocol
	{
		public enum GpType : byte
		{
			Unknown,
			Array = 121,
			Boolean = 111,
			Byte = 98,
			ByteArray = 120,
			ObjectArray = 122,
			Short = 107,
			Float = 102,
			Dictionary = 68,
			Double = 100,
			Hashtable = 104,
			Integer,
			IntegerArray = 110,
			Long = 108,
			String = 115,
			StringArray = 97,
			Custom = 99,
			Null = 42,
			EventData = 101,
			OperationRequest = 113,
			OperationResponse = 112
		}

		private readonly byte[] versionBytes = new byte[]
		{
			1,
			6
		};

		private readonly byte[] memShort = new byte[2];

		private readonly long[] memLongBlock = new long[1];

		private readonly byte[] memLongBlockBytes = new byte[8];

		private static readonly float[] memFloatBlock = new float[1];

		private static readonly byte[] memFloatBlockBytes = new byte[4];

		private readonly double[] memDoubleBlock = new double[1];

		private readonly byte[] memDoubleBlockBytes = new byte[8];

		private readonly byte[] memInteger = new byte[4];

		private readonly byte[] memLong = new byte[8];

		private readonly byte[] memFloat = new byte[4];

		private readonly byte[] memDouble = new byte[8];

		private byte[] memString;

		internal override string protocolType
		{
			get
			{
				return "GpBinaryV16";
			}
		}

		internal override byte[] VersionBytes
		{
			get
			{
				return this.versionBytes;
			}
		}

		private bool SerializeCustom(StreamBuffer dout, object serObject)
		{
			CustomType customType;
			bool flag = Protocol.TypeDict.TryGetValue(serObject.GetType(), out customType);
			bool result;
			if (flag)
			{
				bool flag2 = customType.SerializeStreamFunction == null;
				if (flag2)
				{
					byte[] array = customType.SerializeFunction(serObject);
					dout.WriteByte(99);
					dout.WriteByte(customType.Code);
					this.SerializeShort(dout, (short)array.Length, false);
					dout.Write(array, 0, array.Length);
					result = true;
				}
				else
				{
					dout.WriteByte(99);
					dout.WriteByte(customType.Code);
					long position = dout.Position;
					dout.Position += 2L;
					short num = customType.SerializeStreamFunction(dout, serObject);
					long position2 = dout.Position;
					dout.Position = position;
					this.SerializeShort(dout, num, false);
					dout.Position += (long)num;
					bool flag3 = dout.Position != position2;
					if (flag3)
					{
						throw new Exception(string.Concat(new object[]
						{
							"Serialization failed. Stream position corrupted. Should be ",
							position2,
							" is now: ",
							dout.Position,
							" serializedLength: ",
							num
						}));
					}
					result = true;
				}
			}
			else
			{
				result = false;
			}
			return result;
		}

		private object DeserializeCustom(StreamBuffer din, byte customTypeCode)
		{
			short num = this.DeserializeShort(din);
			CustomType customType;
			bool flag = Protocol.CodeDict.TryGetValue(customTypeCode, out customType);
			object result;
			if (flag)
			{
				bool flag2 = customType.DeserializeStreamFunction == null;
				if (flag2)
				{
					byte[] array = new byte[(int)num];
					din.Read(array, 0, (int)num);
					result = customType.DeserializeFunction(array);
				}
				else
				{
					long position = din.Position;
					object obj = customType.DeserializeStreamFunction(din, num);
					int num2 = (int)(din.Position - position);
					bool flag3 = num2 != (int)num;
					if (flag3)
					{
						din.Position = position + (long)num;
					}
					result = obj;
				}
			}
			else
			{
				byte[] array2 = new byte[(int)num];
				din.Read(array2, 0, (int)num);
				result = array2;
			}
			return result;
		}

		private Type GetTypeOfCode(byte typeCode)
		{
			if (typeCode <= 42)
			{
				if (typeCode == 0 || typeCode == 42)
				{
					Type typeFromHandle = typeof(object);
					return typeFromHandle;
				}
			}
			else
			{
				if (typeCode == 68)
				{
					Type typeFromHandle = typeof(IDictionary);
					return typeFromHandle;
				}
				switch (typeCode)
				{
				case 97:
				{
					Type typeFromHandle = typeof(string[]);
					return typeFromHandle;
				}
				case 98:
				{
					Type typeFromHandle = typeof(byte);
					return typeFromHandle;
				}
				case 99:
				{
					Type typeFromHandle = typeof(CustomType);
					return typeFromHandle;
				}
				case 100:
				{
					Type typeFromHandle = typeof(double);
					return typeFromHandle;
				}
				case 101:
				{
					Type typeFromHandle = typeof(EventData);
					return typeFromHandle;
				}
				case 102:
				{
					Type typeFromHandle = typeof(float);
					return typeFromHandle;
				}
				case 104:
				{
					Type typeFromHandle = typeof(Hashtable);
					return typeFromHandle;
				}
				case 105:
				{
					Type typeFromHandle = typeof(int);
					return typeFromHandle;
				}
				case 107:
				{
					Type typeFromHandle = typeof(short);
					return typeFromHandle;
				}
				case 108:
				{
					Type typeFromHandle = typeof(long);
					return typeFromHandle;
				}
				case 110:
				{
					Type typeFromHandle = typeof(int[]);
					return typeFromHandle;
				}
				case 111:
				{
					Type typeFromHandle = typeof(bool);
					return typeFromHandle;
				}
				case 112:
				{
					Type typeFromHandle = typeof(OperationResponse);
					return typeFromHandle;
				}
				case 113:
				{
					Type typeFromHandle = typeof(OperationRequest);
					return typeFromHandle;
				}
				case 115:
				{
					Type typeFromHandle = typeof(string);
					return typeFromHandle;
				}
				case 120:
				{
					Type typeFromHandle = typeof(byte[]);
					return typeFromHandle;
				}
				case 121:
				{
					Type typeFromHandle = typeof(Array);
					return typeFromHandle;
				}
				case 122:
				{
					Type typeFromHandle = typeof(object[]);
					return typeFromHandle;
				}
				}
			}
			Debug.WriteLine("missing type: " + typeCode);
			throw new Exception("deserialize(): " + typeCode);
		}

		private Protocol16.GpType GetCodeOfType(Type type)
		{
			TypeCode typeCode = Type.GetTypeCode(type);
			TypeCode typeCode2 = typeCode;
			Protocol16.GpType result;
			switch (typeCode2)
			{
			case TypeCode.Boolean:
				result = Protocol16.GpType.Boolean;
				return result;
			case TypeCode.Char:
			case TypeCode.SByte:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
				break;
			case TypeCode.Byte:
				result = Protocol16.GpType.Byte;
				return result;
			case TypeCode.Int16:
				result = Protocol16.GpType.Short;
				return result;
			case TypeCode.Int32:
				result = Protocol16.GpType.Integer;
				return result;
			case TypeCode.Int64:
				result = Protocol16.GpType.Long;
				return result;
			case TypeCode.Single:
				result = Protocol16.GpType.Float;
				return result;
			case TypeCode.Double:
				result = Protocol16.GpType.Double;
				return result;
			default:
				if (typeCode2 == TypeCode.String)
				{
					result = Protocol16.GpType.String;
					return result;
				}
				break;
			}
			bool isArray = type.IsArray;
			if (isArray)
			{
				bool flag = type == typeof(byte[]);
				if (flag)
				{
					result = Protocol16.GpType.ByteArray;
				}
				else
				{
					result = Protocol16.GpType.Array;
				}
			}
			else
			{
				bool flag2 = type == typeof(Hashtable);
				if (flag2)
				{
					result = Protocol16.GpType.Hashtable;
				}
				else
				{
					bool flag3 = type.IsGenericType && typeof(Dictionary<, >) == type.GetGenericTypeDefinition();
					if (flag3)
					{
						result = Protocol16.GpType.Dictionary;
					}
					else
					{
						bool flag4 = type == typeof(EventData);
						if (flag4)
						{
							result = Protocol16.GpType.EventData;
						}
						else
						{
							bool flag5 = type == typeof(OperationRequest);
							if (flag5)
							{
								result = Protocol16.GpType.OperationRequest;
							}
							else
							{
								bool flag6 = type == typeof(OperationResponse);
								if (flag6)
								{
									result = Protocol16.GpType.OperationResponse;
								}
								else
								{
									result = Protocol16.GpType.Unknown;
								}
							}
						}
					}
				}
			}
			return result;
		}

		private Array CreateArrayByType(byte arrayType, short length)
		{
			return Array.CreateInstance(this.GetTypeOfCode(arrayType), (int)length);
		}

		private void SerializeOperationRequest(StreamBuffer stream, OperationRequest serObject, bool setType)
		{
			this.SerializeOperationRequest(stream, serObject.OperationCode, serObject.Parameters, setType);
		}

		public override void SerializeOperationRequest(StreamBuffer stream, byte operationCode, Dictionary<byte, object> parameters, bool setType)
		{
			if (setType)
			{
				stream.WriteByte(113);
			}
			stream.WriteByte(operationCode);
			this.SerializeParameterTable(stream, parameters);
		}

		public override OperationRequest DeserializeOperationRequest(StreamBuffer din)
		{
			return new OperationRequest
			{
				OperationCode = this.DeserializeByte(din),
				Parameters = this.DeserializeParameterTable(din)
			};
		}

		public override void SerializeOperationResponse(StreamBuffer stream, OperationResponse serObject, bool setType)
		{
			if (setType)
			{
				stream.WriteByte(112);
			}
			stream.WriteByte(serObject.OperationCode);
			this.SerializeShort(stream, serObject.ReturnCode, false);
			bool flag = string.IsNullOrEmpty(serObject.DebugMessage);
			if (flag)
			{
				stream.WriteByte(42);
			}
			else
			{
				this.SerializeString(stream, serObject.DebugMessage, false);
			}
			this.SerializeParameterTable(stream, serObject.Parameters);
		}

		public override OperationResponse DeserializeOperationResponse(StreamBuffer stream)
		{
			return new OperationResponse
			{
				OperationCode = this.DeserializeByte(stream),
				ReturnCode = this.DeserializeShort(stream),
				DebugMessage = (this.Deserialize(stream, this.DeserializeByte(stream)) as string),
				Parameters = this.DeserializeParameterTable(stream)
			};
		}

		public override void SerializeEventData(StreamBuffer stream, EventData serObject, bool setType)
		{
			if (setType)
			{
				stream.WriteByte(101);
			}
			stream.WriteByte(serObject.Code);
			this.SerializeParameterTable(stream, serObject.Parameters);
		}

		public override EventData DeserializeEventData(StreamBuffer din)
		{
			return new EventData
			{
				Code = this.DeserializeByte(din),
				Parameters = this.DeserializeParameterTable(din)
			};
		}

		private void SerializeParameterTable(StreamBuffer stream, Dictionary<byte, object> parameters)
		{
			bool flag = parameters == null || parameters.Count == 0;
			if (flag)
			{
				this.SerializeShort(stream, 0, false);
			}
			else
			{
				this.SerializeShort(stream, (short)parameters.Count, false);
				foreach (KeyValuePair<byte, object> current in parameters)
				{
					stream.WriteByte(current.Key);
					this.Serialize(stream, current.Value, true);
				}
			}
		}

		private Dictionary<byte, object> DeserializeParameterTable(StreamBuffer stream)
		{
			short num = this.DeserializeShort(stream);
			Dictionary<byte, object> dictionary = new Dictionary<byte, object>((int)num);
			for (int i = 0; i < (int)num; i++)
			{
				byte key = (byte)stream.ReadByte();
				object value = this.Deserialize(stream, (byte)stream.ReadByte());
				dictionary[key] = value;
			}
			return dictionary;
		}

		public override void Serialize(StreamBuffer dout, object serObject, bool setType)
		{
			bool flag = serObject == null;
			if (flag)
			{
				if (setType)
				{
					dout.WriteByte(42);
				}
			}
			else
			{
				Protocol16.GpType codeOfType = this.GetCodeOfType(serObject.GetType());
				Protocol16.GpType gpType = codeOfType;
				if (gpType != Protocol16.GpType.Dictionary)
				{
					switch (gpType)
					{
					case Protocol16.GpType.Byte:
						this.SerializeByte(dout, (byte)serObject, setType);
						return;
					case Protocol16.GpType.Double:
						this.SerializeDouble(dout, (double)serObject, setType);
						return;
					case Protocol16.GpType.EventData:
						this.SerializeEventData(dout, (EventData)serObject, setType);
						return;
					case Protocol16.GpType.Float:
						this.SerializeFloat(dout, (float)serObject, setType);
						return;
					case Protocol16.GpType.Hashtable:
						this.SerializeHashTable(dout, (Hashtable)serObject, setType);
						return;
					case Protocol16.GpType.Integer:
						this.SerializeInteger(dout, (int)serObject, setType);
						return;
					case Protocol16.GpType.Short:
						this.SerializeShort(dout, (short)serObject, setType);
						return;
					case Protocol16.GpType.Long:
						this.SerializeLong(dout, (long)serObject, setType);
						return;
					case Protocol16.GpType.Boolean:
						this.SerializeBoolean(dout, (bool)serObject, setType);
						return;
					case Protocol16.GpType.OperationResponse:
						this.SerializeOperationResponse(dout, (OperationResponse)serObject, setType);
						return;
					case Protocol16.GpType.OperationRequest:
						this.SerializeOperationRequest(dout, (OperationRequest)serObject, setType);
						return;
					case Protocol16.GpType.String:
						this.SerializeString(dout, (string)serObject, setType);
						return;
					case Protocol16.GpType.ByteArray:
						this.SerializeByteArray(dout, (byte[])serObject, setType);
						return;
					case Protocol16.GpType.Array:
					{
						bool flag2 = serObject is int[];
						if (flag2)
						{
							this.SerializeIntArrayOptimized(dout, (int[])serObject, setType);
						}
						else
						{
							bool flag3 = serObject.GetType().GetElementType() == typeof(object);
							if (flag3)
							{
								this.SerializeObjectArray(dout, serObject as object[], setType);
							}
							else
							{
								this.SerializeArray(dout, (Array)serObject, setType);
							}
						}
						return;
					}
					}
					bool flag4 = serObject is ArraySegment<byte>;
					if (flag4)
					{
						ArraySegment<byte> arraySegment = (ArraySegment<byte>)serObject;
						this.SerializeByteArraySegment(dout, arraySegment.Array, arraySegment.Offset, arraySegment.Count, setType);
					}
					else
					{
						bool flag5 = !this.SerializeCustom(dout, serObject);
						if (flag5)
						{
							throw new Exception("cannot serialize(): " + serObject.GetType());
						}
					}
				}
				else
				{
					this.SerializeDictionary(dout, (IDictionary)serObject, setType);
				}
			}
		}

		private void SerializeByte(StreamBuffer dout, byte serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(98);
			}
			dout.WriteByte(serObject);
		}

		private void SerializeBoolean(StreamBuffer dout, bool serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(111);
			}
			dout.WriteByte(serObject ? 1 : 0);
		}

		public override void SerializeShort(StreamBuffer dout, short serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(107);
			}
			byte[] obj = this.memShort;
			lock (obj)
			{
				byte[] array = this.memShort;
				array[0] = (byte)(serObject >> 8);
				array[1] = (byte)serObject;
				dout.Write(array, 0, 2);
			}
		}

		private void SerializeInteger(StreamBuffer dout, int serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(105);
			}
			byte[] obj = this.memInteger;
			lock (obj)
			{
				byte[] array = this.memInteger;
				array[0] = (byte)(serObject >> 24);
				array[1] = (byte)(serObject >> 16);
				array[2] = (byte)(serObject >> 8);
				array[3] = (byte)serObject;
				dout.Write(array, 0, 4);
			}
		}

		private void SerializeLong(StreamBuffer dout, long serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(108);
			}
			long[] obj = this.memLongBlock;
			lock (obj)
			{
				this.memLongBlock[0] = serObject;
				Buffer.BlockCopy(this.memLongBlock, 0, this.memLongBlockBytes, 0, 8);
				byte[] array = this.memLongBlockBytes;
				bool isLittleEndian = BitConverter.IsLittleEndian;
				if (isLittleEndian)
				{
					byte b = array[0];
					byte b2 = array[1];
					byte b3 = array[2];
					byte b4 = array[3];
					array[0] = array[7];
					array[1] = array[6];
					array[2] = array[5];
					array[3] = array[4];
					array[4] = b4;
					array[5] = b3;
					array[6] = b2;
					array[7] = b;
				}
				dout.Write(array, 0, 8);
			}
		}

		private void SerializeFloat(StreamBuffer dout, float serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(102);
			}
			byte[] obj = Protocol16.memFloatBlockBytes;
			lock (obj)
			{
				Protocol16.memFloatBlock[0] = serObject;
				Buffer.BlockCopy(Protocol16.memFloatBlock, 0, Protocol16.memFloatBlockBytes, 0, 4);
				bool isLittleEndian = BitConverter.IsLittleEndian;
				if (isLittleEndian)
				{
					byte b = Protocol16.memFloatBlockBytes[0];
					byte b2 = Protocol16.memFloatBlockBytes[1];
					Protocol16.memFloatBlockBytes[0] = Protocol16.memFloatBlockBytes[3];
					Protocol16.memFloatBlockBytes[1] = Protocol16.memFloatBlockBytes[2];
					Protocol16.memFloatBlockBytes[2] = b2;
					Protocol16.memFloatBlockBytes[3] = b;
				}
				dout.Write(Protocol16.memFloatBlockBytes, 0, 4);
			}
		}

		private void SerializeDouble(StreamBuffer dout, double serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(100);
			}
			byte[] obj = this.memDoubleBlockBytes;
			lock (obj)
			{
				this.memDoubleBlock[0] = serObject;
				Buffer.BlockCopy(this.memDoubleBlock, 0, this.memDoubleBlockBytes, 0, 8);
				byte[] array = this.memDoubleBlockBytes;
				bool isLittleEndian = BitConverter.IsLittleEndian;
				if (isLittleEndian)
				{
					byte b = array[0];
					byte b2 = array[1];
					byte b3 = array[2];
					byte b4 = array[3];
					array[0] = array[7];
					array[1] = array[6];
					array[2] = array[5];
					array[3] = array[4];
					array[4] = b4;
					array[5] = b3;
					array[6] = b2;
					array[7] = b;
				}
				dout.Write(array, 0, 8);
			}
		}

		public override void SerializeString(StreamBuffer dout, string serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(115);
			}
			byte[] bytes = Encoding.UTF8.GetBytes(serObject);
			bool flag = bytes.Length > 32767;
			if (flag)
			{
				throw new NotSupportedException("Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: " + bytes.Length);
			}
			this.SerializeShort(dout, (short)bytes.Length, false);
			dout.Write(bytes, 0, bytes.Length);
		}

		private void SerializeArray(StreamBuffer dout, Array serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(121);
			}
			bool flag = serObject.Length > 32767;
			if (flag)
			{
				throw new NotSupportedException("String[] that exceed 32767 (short.MaxValue) entries are not supported. Yours is: " + serObject.Length);
			}
			this.SerializeShort(dout, (short)serObject.Length, false);
			Type elementType = serObject.GetType().GetElementType();
			Protocol16.GpType codeOfType = this.GetCodeOfType(elementType);
			bool flag2 = codeOfType > Protocol16.GpType.Unknown;
			if (flag2)
			{
				dout.WriteByte((byte)codeOfType);
				bool flag3 = codeOfType == Protocol16.GpType.Dictionary;
				if (flag3)
				{
					bool setKeyType;
					bool setValueType;
					this.SerializeDictionaryHeader(dout, serObject, out setKeyType, out setValueType);
					for (int i = 0; i < serObject.Length; i++)
					{
						object value = serObject.GetValue(i);
						this.SerializeDictionaryElements(dout, value, setKeyType, setValueType);
					}
				}
				else
				{
					for (int j = 0; j < serObject.Length; j++)
					{
						object value2 = serObject.GetValue(j);
						this.Serialize(dout, value2, false);
					}
				}
			}
			else
			{
				CustomType customType;
				bool flag4 = Protocol.TypeDict.TryGetValue(elementType, out customType);
				if (!flag4)
				{
					throw new NotSupportedException("cannot serialize array of type " + elementType);
				}
				dout.WriteByte(99);
				dout.WriteByte(customType.Code);
				for (int k = 0; k < serObject.Length; k++)
				{
					object value3 = serObject.GetValue(k);
					bool flag5 = customType.SerializeStreamFunction == null;
					if (flag5)
					{
						byte[] array = customType.SerializeFunction(value3);
						this.SerializeShort(dout, (short)array.Length, false);
						dout.Write(array, 0, array.Length);
					}
					else
					{
						long position = dout.Position;
						dout.Position += 2L;
						short num = customType.SerializeStreamFunction(dout, value3);
						long position2 = dout.Position;
						dout.Position = position;
						this.SerializeShort(dout, num, false);
						dout.Position += (long)num;
						bool flag6 = dout.Position != position2;
						if (flag6)
						{
							throw new Exception(string.Concat(new object[]
							{
								"Serialization failed. Stream position corrupted. Should be ",
								position2,
								" is now: ",
								dout.Position,
								" serializedLength: ",
								num
							}));
						}
					}
				}
			}
		}

		private void SerializeByteArray(StreamBuffer dout, byte[] serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(120);
			}
			this.SerializeInteger(dout, serObject.Length, false);
			dout.Write(serObject, 0, serObject.Length);
		}

		private void SerializeByteArraySegment(StreamBuffer dout, byte[] serObject, int offset, int count, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(120);
			}
			this.SerializeInteger(dout, count, false);
			dout.Write(serObject, offset, count);
		}

		private void SerializeIntArrayOptimized(StreamBuffer inWriter, int[] serObject, bool setType)
		{
			if (setType)
			{
				inWriter.WriteByte(121);
			}
			this.SerializeShort(inWriter, (short)serObject.Length, false);
			inWriter.WriteByte(105);
			byte[] array = new byte[serObject.Length * 4];
			int num = 0;
			for (int i = 0; i < serObject.Length; i++)
			{
				array[num++] = (byte)(serObject[i] >> 24);
				array[num++] = (byte)(serObject[i] >> 16);
				array[num++] = (byte)(serObject[i] >> 8);
				array[num++] = (byte)serObject[i];
			}
			inWriter.Write(array, 0, array.Length);
		}

		private void SerializeStringArray(StreamBuffer dout, string[] serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(97);
			}
			this.SerializeShort(dout, (short)serObject.Length, false);
			for (int i = 0; i < serObject.Length; i++)
			{
				this.SerializeString(dout, serObject[i], false);
			}
		}

		private void SerializeObjectArray(StreamBuffer dout, object[] objects, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(122);
			}
			this.SerializeShort(dout, (short)objects.Length, false);
			for (int i = 0; i < objects.Length; i++)
			{
				object serObject = objects[i];
				this.Serialize(dout, serObject, true);
			}
		}

		private void SerializeHashTable(StreamBuffer dout, Hashtable serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(104);
			}
			this.SerializeShort(dout, (short)serObject.Count, false);
			Dictionary<object, object>.KeyCollection keys = serObject.Keys;
			foreach (object current in keys)
			{
				this.Serialize(dout, current, true);
				this.Serialize(dout, serObject[current], true);
			}
		}

		private void SerializeDictionary(StreamBuffer dout, IDictionary serObject, bool setType)
		{
			if (setType)
			{
				dout.WriteByte(68);
			}
			bool setKeyType;
			bool setValueType;
			this.SerializeDictionaryHeader(dout, serObject, out setKeyType, out setValueType);
			this.SerializeDictionaryElements(dout, serObject, setKeyType, setValueType);
		}

		private void SerializeDictionaryHeader(StreamBuffer writer, Type dictType)
		{
			bool flag;
			bool flag2;
			this.SerializeDictionaryHeader(writer, dictType, out flag, out flag2);
		}

		private void SerializeDictionaryHeader(StreamBuffer writer, object dict, out bool setKeyType, out bool setValueType)
		{
			Type[] genericArguments = dict.GetType().GetGenericArguments();
			setKeyType = (genericArguments[0] == typeof(object));
			setValueType = (genericArguments[1] == typeof(object));
			bool flag = setKeyType;
			if (flag)
			{
				writer.WriteByte(0);
			}
			else
			{
				Protocol16.GpType codeOfType = this.GetCodeOfType(genericArguments[0]);
				bool flag2 = codeOfType == Protocol16.GpType.Unknown || codeOfType == Protocol16.GpType.Dictionary;
				if (flag2)
				{
					throw new Exception("Unexpected - cannot serialize Dictionary with key type: " + genericArguments[0]);
				}
				writer.WriteByte((byte)codeOfType);
			}
			bool flag3 = setValueType;
			if (flag3)
			{
				writer.WriteByte(0);
			}
			else
			{
				Protocol16.GpType codeOfType2 = this.GetCodeOfType(genericArguments[1]);
				bool flag4 = codeOfType2 == Protocol16.GpType.Unknown;
				if (flag4)
				{
					throw new Exception("Unexpected - cannot serialize Dictionary with value type: " + genericArguments[0]);
				}
				writer.WriteByte((byte)codeOfType2);
				bool flag5 = codeOfType2 == Protocol16.GpType.Dictionary;
				if (flag5)
				{
					this.SerializeDictionaryHeader(writer, genericArguments[1]);
				}
			}
		}

		private void SerializeDictionaryElements(StreamBuffer writer, object dict, bool setKeyType, bool setValueType)
		{
			IDictionary dictionary = (IDictionary)dict;
			this.SerializeShort(writer, (short)dictionary.Count, false);
			foreach (DictionaryEntry dictionaryEntry in dictionary)
			{
				bool flag = !setValueType && dictionaryEntry.Value == null;
				if (flag)
				{
					throw new Exception("Can't serialize null in Dictionary with specific value-type.");
				}
				bool flag2 = !setKeyType && dictionaryEntry.Key == null;
				if (flag2)
				{
					throw new Exception("Can't serialize null in Dictionary with specific key-type.");
				}
				this.Serialize(writer, dictionaryEntry.Key, setKeyType);
				this.Serialize(writer, dictionaryEntry.Value, setValueType);
			}
		}

		public override object Deserialize(StreamBuffer din, byte type)
		{
			if (type <= 42)
			{
				if (type == 0 || type == 42)
				{
					object result = null;
					return result;
				}
			}
			else
			{
				if (type == 68)
				{
					object result = this.DeserializeDictionary(din);
					return result;
				}
				switch (type)
				{
				case 97:
				{
					object result = this.DeserializeStringArray(din);
					return result;
				}
				case 98:
				{
					object result = this.DeserializeByte(din);
					return result;
				}
				case 99:
				{
					byte customTypeCode = (byte)din.ReadByte();
					object result = this.DeserializeCustom(din, customTypeCode);
					return result;
				}
				case 100:
				{
					object result = this.DeserializeDouble(din);
					return result;
				}
				case 101:
				{
					object result = this.DeserializeEventData(din);
					return result;
				}
				case 102:
				{
					object result = this.DeserializeFloat(din);
					return result;
				}
				case 104:
				{
					object result = this.DeserializeHashTable(din);
					return result;
				}
				case 105:
				{
					object result = this.DeserializeInteger(din);
					return result;
				}
				case 107:
				{
					object result = this.DeserializeShort(din);
					return result;
				}
				case 108:
				{
					object result = this.DeserializeLong(din);
					return result;
				}
				case 110:
				{
					object result = this.DeserializeIntArray(din);
					return result;
				}
				case 111:
				{
					object result = this.DeserializeBoolean(din);
					return result;
				}
				case 112:
				{
					object result = this.DeserializeOperationResponse(din);
					return result;
				}
				case 113:
				{
					object result = this.DeserializeOperationRequest(din);
					return result;
				}
				case 115:
				{
					object result = this.DeserializeString(din);
					return result;
				}
				case 120:
				{
					object result = this.DeserializeByteArray(din);
					return result;
				}
				case 121:
				{
					object result = this.DeserializeArray(din);
					return result;
				}
				case 122:
				{
					object result = this.DeserializeObjectArray(din);
					return result;
				}
				}
			}
			throw new Exception(string.Concat(new object[]
			{
				"Deserialize(): ",
				type,
				" pos: ",
				din.Position,
				" bytes: ",
				din.Length,
				". ",
				SupportClass.ByteArrayToString(din.GetBuffer())
			}));
		}

		public override byte DeserializeByte(StreamBuffer din)
		{
			return (byte)din.ReadByte();
		}

		private bool DeserializeBoolean(StreamBuffer din)
		{
			return din.ReadByte() != 0;
		}

		public override short DeserializeShort(StreamBuffer din)
		{
			byte[] obj = this.memShort;
			short result;
			lock (obj)
			{
				byte[] array = this.memShort;
				din.Read(array, 0, 2);
				result = (short)((int)array[0] << 8 | (int)array[1]);
			}
			return result;
		}

		private int DeserializeInteger(StreamBuffer din)
		{
			byte[] obj = this.memInteger;
			int result;
			lock (obj)
			{
				byte[] array = this.memInteger;
				din.Read(array, 0, 4);
				result = ((int)array[0] << 24 | (int)array[1] << 16 | (int)array[2] << 8 | (int)array[3]);
			}
			return result;
		}

		private long DeserializeLong(StreamBuffer din)
		{
			byte[] obj = this.memLong;
			long result;
			lock (obj)
			{
				byte[] array = this.memLong;
				din.Read(array, 0, 8);
				bool isLittleEndian = BitConverter.IsLittleEndian;
				if (isLittleEndian)
				{
					result = (long)((ulong)array[0] << 56 | (ulong)array[1] << 48 | (ulong)array[2] << 40 | (ulong)array[3] << 32 | (ulong)array[4] << 24 | (ulong)array[5] << 16 | (ulong)array[6] << 8 | (ulong)array[7]);
				}
				else
				{
					result = BitConverter.ToInt64(array, 0);
				}
			}
			return result;
		}

		private float DeserializeFloat(StreamBuffer din)
		{
			byte[] obj = this.memFloat;
			float result;
			lock (obj)
			{
				byte[] array = this.memFloat;
				din.Read(array, 0, 4);
				bool isLittleEndian = BitConverter.IsLittleEndian;
				if (isLittleEndian)
				{
					byte b = array[0];
					byte b2 = array[1];
					array[0] = array[3];
					array[1] = array[2];
					array[2] = b2;
					array[3] = b;
				}
				result = BitConverter.ToSingle(array, 0);
			}
			return result;
		}

		private double DeserializeDouble(StreamBuffer din)
		{
			byte[] obj = this.memDouble;
			double result;
			lock (obj)
			{
				byte[] array = this.memDouble;
				din.Read(array, 0, 8);
				bool isLittleEndian = BitConverter.IsLittleEndian;
				if (isLittleEndian)
				{
					byte b = array[0];
					byte b2 = array[1];
					byte b3 = array[2];
					byte b4 = array[3];
					array[0] = array[7];
					array[1] = array[6];
					array[2] = array[5];
					array[3] = array[4];
					array[4] = b4;
					array[5] = b3;
					array[6] = b2;
					array[7] = b;
				}
				result = BitConverter.ToDouble(array, 0);
			}
			return result;
		}

		private string DeserializeString(StreamBuffer din)
		{
			short num = this.DeserializeShort(din);
			bool flag = num == 0;
			string result;
			if (flag)
			{
				result = string.Empty;
			}
			else
			{
				bool flag2 = this.memString == null || this.memString.Length < (int)num;
				if (flag2)
				{
					this.memString = new byte[(int)num];
				}
				din.Read(this.memString, 0, (int)num);
				result = Encoding.UTF8.GetString(this.memString, 0, (int)num);
			}
			return result;
		}

		private Array DeserializeArray(StreamBuffer din)
		{
			short num = this.DeserializeShort(din);
			byte b = (byte)din.ReadByte();
			bool flag = b == 121;
			Array array2;
			Array result;
			if (flag)
			{
				Array array = this.DeserializeArray(din);
				Type type = array.GetType();
				array2 = Array.CreateInstance(type, (int)num);
				array2.SetValue(array, 0);
				for (short num2 = 1; num2 < num; num2 += 1)
				{
					array = this.DeserializeArray(din);
					array2.SetValue(array, (int)num2);
				}
			}
			else
			{
				bool flag2 = b == 120;
				if (flag2)
				{
					array2 = Array.CreateInstance(typeof(byte[]), (int)num);
					for (short num3 = 0; num3 < num; num3 += 1)
					{
						Array value = this.DeserializeByteArray(din);
						array2.SetValue(value, (int)num3);
					}
				}
				else
				{
					bool flag3 = b == 99;
					if (flag3)
					{
						byte b2 = (byte)din.ReadByte();
						CustomType customType;
						bool flag4 = Protocol.CodeDict.TryGetValue(b2, out customType);
						if (!flag4)
						{
							throw new Exception("Cannot find deserializer for custom type: " + b2);
						}
						array2 = Array.CreateInstance(customType.Type, (int)num);
						for (int i = 0; i < (int)num; i++)
						{
							short num4 = this.DeserializeShort(din);
							bool flag5 = customType.DeserializeStreamFunction == null;
							if (flag5)
							{
								byte[] array3 = new byte[(int)num4];
								din.Read(array3, 0, (int)num4);
								array2.SetValue(customType.DeserializeFunction(array3), i);
							}
							else
							{
								array2.SetValue(customType.DeserializeStreamFunction(din, num4), i);
							}
						}
					}
					else
					{
						bool flag6 = b == 68;
						if (flag6)
						{
							Array array4 = null;
							this.DeserializeDictionaryArray(din, num, out array4);
							result = array4;
							return result;
						}
						array2 = this.CreateArrayByType(b, num);
						for (short num5 = 0; num5 < num; num5 += 1)
						{
							array2.SetValue(this.Deserialize(din, b), (int)num5);
						}
					}
				}
			}
			result = array2;
			return result;
		}

		private byte[] DeserializeByteArray(StreamBuffer din)
		{
			int num = this.DeserializeInteger(din);
			byte[] array = new byte[num];
			din.Read(array, 0, num);
			return array;
		}

		private int[] DeserializeIntArray(StreamBuffer din)
		{
			int num = this.DeserializeInteger(din);
			int[] array = new int[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = this.DeserializeInteger(din);
			}
			return array;
		}

		private string[] DeserializeStringArray(StreamBuffer din)
		{
			int num = (int)this.DeserializeShort(din);
			string[] array = new string[num];
			for (int i = 0; i < num; i++)
			{
				array[i] = this.DeserializeString(din);
			}
			return array;
		}

		private object[] DeserializeObjectArray(StreamBuffer din)
		{
			short num = this.DeserializeShort(din);
			object[] array = new object[(int)num];
			for (int i = 0; i < (int)num; i++)
			{
				byte type = (byte)din.ReadByte();
				array[i] = this.Deserialize(din, type);
			}
			return array;
		}

		private Hashtable DeserializeHashTable(StreamBuffer din)
		{
			int num = (int)this.DeserializeShort(din);
			Hashtable hashtable = new Hashtable(num);
			for (int i = 0; i < num; i++)
			{
				object key = this.Deserialize(din, (byte)din.ReadByte());
				object value = this.Deserialize(din, (byte)din.ReadByte());
				hashtable[key] = value;
			}
			return hashtable;
		}

		private IDictionary DeserializeDictionary(StreamBuffer din)
		{
			byte b = (byte)din.ReadByte();
			byte b2 = (byte)din.ReadByte();
			int num = (int)this.DeserializeShort(din);
			bool flag = b == 0 || b == 42;
			bool flag2 = b2 == 0 || b2 == 42;
			Type typeOfCode = this.GetTypeOfCode(b);
			Type typeOfCode2 = this.GetTypeOfCode(b2);
			Type type = typeof(Dictionary<, >).MakeGenericType(new Type[]
			{
				typeOfCode,
				typeOfCode2
			});
			IDictionary dictionary = Activator.CreateInstance(type) as IDictionary;
			for (int i = 0; i < num; i++)
			{
				object key = this.Deserialize(din, flag ? ((byte)din.ReadByte()) : b);
				object value = this.Deserialize(din, flag2 ? ((byte)din.ReadByte()) : b2);
				dictionary.Add(key, value);
			}
			return dictionary;
		}

		private bool DeserializeDictionaryArray(StreamBuffer din, short size, out Array arrayResult)
		{
			byte b;
			byte b2;
			Type type = this.DeserializeDictionaryType(din, out b, out b2);
			arrayResult = Array.CreateInstance(type, (int)size);
			bool result;
			for (short num = 0; num < size; num += 1)
			{
				IDictionary dictionary = Activator.CreateInstance(type) as IDictionary;
				bool flag = dictionary == null;
				if (flag)
				{
					result = false;
					return result;
				}
				short num2 = this.DeserializeShort(din);
				for (int i = 0; i < (int)num2; i++)
				{
					bool flag2 = b > 0;
					object key;
					if (flag2)
					{
						key = this.Deserialize(din, b);
					}
					else
					{
						byte type2 = (byte)din.ReadByte();
						key = this.Deserialize(din, type2);
					}
					bool flag3 = b2 > 0;
					object value;
					if (flag3)
					{
						value = this.Deserialize(din, b2);
					}
					else
					{
						byte type3 = (byte)din.ReadByte();
						value = this.Deserialize(din, type3);
					}
					dictionary.Add(key, value);
				}
				arrayResult.SetValue(dictionary, (int)num);
			}
			result = true;
			return result;
		}

		private Type DeserializeDictionaryType(StreamBuffer reader, out byte keyTypeCode, out byte valTypeCode)
		{
			keyTypeCode = (byte)reader.ReadByte();
			valTypeCode = (byte)reader.ReadByte();
			Protocol16.GpType gpType = (Protocol16.GpType)keyTypeCode;
			Protocol16.GpType gpType2 = (Protocol16.GpType)valTypeCode;
			bool flag = gpType == Protocol16.GpType.Unknown;
			Type type;
			if (flag)
			{
				type = typeof(object);
			}
			else
			{
				type = this.GetTypeOfCode(keyTypeCode);
			}
			bool flag2 = gpType2 == Protocol16.GpType.Unknown;
			Type type2;
			if (flag2)
			{
				type2 = typeof(object);
			}
			else
			{
				type2 = this.GetTypeOfCode(valTypeCode);
			}
			return typeof(Dictionary<, >).MakeGenericType(new Type[]
			{
				type,
				type2
			});
		}
	}
}
