using System;
using System.IO;

namespace ExitGames.Client.Photon
{
	public class StreamBuffer : Stream
	{
		private const int DefaultInitialSize = 0;

		private int pos;

		private int len;

		private byte[] buf;

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanSeek
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public override long Length
		{
			get
			{
				return (long)this.len;
			}
		}

		public override long Position
		{
			get
			{
				return (long)this.pos;
			}
			set
			{
				this.pos = (int)value;
				bool flag = this.len < this.pos;
				if (flag)
				{
					this.len = this.pos;
					this.CheckSize(this.len);
				}
			}
		}

		public StreamBuffer(int size = 0)
		{
			this.buf = new byte[size];
		}

		public StreamBuffer(byte[] buf)
		{
			this.buf = buf;
			this.len = buf.Length;
		}

		public byte[] ToArray()
		{
			byte[] array = new byte[this.len];
			Buffer.BlockCopy(this.buf, 0, array, 0, this.len);
			return array;
		}

		public byte[] ToArrayFromPos()
		{
			int num = this.len - this.pos;
			bool flag = num <= 0;
			byte[] result;
			if (flag)
			{
				result = new byte[0];
			}
			else
			{
				byte[] array = new byte[num];
				Buffer.BlockCopy(this.buf, this.pos, array, 0, num);
				result = array;
			}
			return result;
		}

		public void Compact()
		{
			long num = this.Length - this.Position;
			bool flag = num > 0L;
			if (flag)
			{
				Buffer.BlockCopy(this.buf, (int)this.Position, this.buf, 0, (int)num);
			}
			this.Position = 0L;
			this.SetLength(num);
		}

		public byte[] GetBuffer()
		{
			return this.buf;
		}

		public byte[] GetBufferAndAdvance(int length, out int offset)
		{
			offset = (int)this.Position;
			this.Position += (long)length;
			return this.buf;
		}

		public override void Flush()
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			int num;
			switch (origin)
			{
			case SeekOrigin.Begin:
				num = (int)offset;
				break;
			case SeekOrigin.Current:
				num = this.pos + (int)offset;
				break;
			case SeekOrigin.End:
				num = this.len + (int)offset;
				break;
			default:
				throw new ArgumentException("Invalid seek origin");
			}
			bool flag = num < 0;
			if (flag)
			{
				throw new ArgumentException("Seek before begin");
			}
			bool flag2 = num > this.len;
			if (flag2)
			{
				throw new ArgumentException("Seek after end");
			}
			this.pos = num;
			return (long)this.pos;
		}

		public override void SetLength(long value)
		{
			this.len = (int)value;
			this.CheckSize(this.len);
			bool flag = this.pos > this.len;
			if (flag)
			{
				this.pos = this.len;
			}
		}

		public void SetCapacityMinimum(int neededSize)
		{
			this.CheckSize(neededSize);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int num = this.len - this.pos;
			bool flag = num <= 0;
			int result;
			if (flag)
			{
				result = 0;
			}
			else
			{
				bool flag2 = count > num;
				if (flag2)
				{
					count = num;
				}
				Buffer.BlockCopy(this.buf, this.pos, buffer, offset, count);
				this.pos += count;
				result = count;
			}
			return result;
		}

		public override void Write(byte[] buffer, int srcOffset, int count)
		{
			int num = this.pos + count;
			this.CheckSize(num);
			bool flag = num > this.len;
			if (flag)
			{
				this.len = num;
			}
			Buffer.BlockCopy(buffer, srcOffset, this.buf, this.pos, count);
			this.pos = num;
		}

		public override int ReadByte()
		{
			bool flag = this.pos >= this.len;
			int result;
			if (flag)
			{
				result = -1;
			}
			else
			{
				byte[] arg_32_0 = this.buf;
				int num = this.pos;
				this.pos = num + 1;
				result = arg_32_0[num];
			}
			return result;
		}

		public override void WriteByte(byte value)
		{
			bool flag = this.pos >= this.len;
			if (flag)
			{
				this.len = this.pos + 1;
				this.CheckSize(this.len);
			}
			byte[] arg_4B_0 = this.buf;
			int num = this.pos;
			this.pos = num + 1;
			arg_4B_0[num] = value;
		}

		public void WriteBytes(byte v0, byte v1)
		{
			int num = this.pos + 2;
			bool flag = this.len < num;
			if (flag)
			{
				this.len = num;
				this.CheckSize(this.len);
			}
			byte[] arg_45_0 = this.buf;
			int num2 = this.pos;
			this.pos = num2 + 1;
			arg_45_0[num2] = v0;
			byte[] arg_5E_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_5E_0[num2] = v1;
		}

		public void WriteBytes(byte v0, byte v1, byte v2)
		{
			int num = this.pos + 3;
			bool flag = this.len < num;
			if (flag)
			{
				this.len = num;
				this.CheckSize(this.len);
			}
			byte[] arg_45_0 = this.buf;
			int num2 = this.pos;
			this.pos = num2 + 1;
			arg_45_0[num2] = v0;
			byte[] arg_5E_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_5E_0[num2] = v1;
			byte[] arg_77_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_77_0[num2] = v2;
		}

		public void WriteBytes(byte v0, byte v1, byte v2, byte v3)
		{
			int num = this.pos + 4;
			bool flag = this.len < num;
			if (flag)
			{
				this.len = num;
				this.CheckSize(this.len);
			}
			byte[] arg_45_0 = this.buf;
			int num2 = this.pos;
			this.pos = num2 + 1;
			arg_45_0[num2] = v0;
			byte[] arg_5E_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_5E_0[num2] = v1;
			byte[] arg_77_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_77_0[num2] = v2;
			byte[] arg_91_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_91_0[num2] = v3;
		}

		public void WriteBytes(byte v0, byte v1, byte v2, byte v3, byte v4, byte v5, byte v6, byte v7)
		{
			int num = this.pos + 8;
			bool flag = this.len < num;
			if (flag)
			{
				this.len = num;
				this.CheckSize(this.len);
			}
			byte[] arg_45_0 = this.buf;
			int num2 = this.pos;
			this.pos = num2 + 1;
			arg_45_0[num2] = v0;
			byte[] arg_5E_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_5E_0[num2] = v1;
			byte[] arg_77_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_77_0[num2] = v2;
			byte[] arg_91_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_91_0[num2] = v3;
			byte[] arg_AB_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_AB_0[num2] = v4;
			byte[] arg_C5_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_C5_0[num2] = v5;
			byte[] arg_DF_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_DF_0[num2] = v6;
			byte[] arg_F9_0 = this.buf;
			num2 = this.pos;
			this.pos = num2 + 1;
			arg_F9_0[num2] = v7;
		}

		private bool CheckSize(int size)
		{
			bool flag = size <= this.buf.Length;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				int num = this.buf.Length;
				bool flag2 = num == 0;
				if (flag2)
				{
					num = 1;
				}
				while (size > num)
				{
					num *= 2;
				}
				byte[] dst = new byte[num];
				Buffer.BlockCopy(this.buf, 0, dst, 0, this.buf.Length);
				this.buf = dst;
				result = true;
			}
			return result;
		}
	}
}
