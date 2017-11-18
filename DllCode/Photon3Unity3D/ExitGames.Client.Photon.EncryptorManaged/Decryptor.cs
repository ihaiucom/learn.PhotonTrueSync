using System;
using System.IO;
using System.Security.Cryptography;

namespace ExitGames.Client.Photon.EncryptorManaged
{
	public class Decryptor : CryptoBase
	{
		private readonly byte[] IV = new byte[16];

		private readonly byte[] readBuffer = new byte[16];

		public byte[] DecryptBufferWithIV(byte[] data, int offset, int len, out int outLen)
		{
			Buffer.BlockCopy(data, offset, this.IV, 0, 16);
			this.encryptor.IV = this.IV;
			byte[] result;
			using (ICryptoTransform cryptoTransform = this.encryptor.CreateDecryptor())
			{
				using (MemoryStream memoryStream = new MemoryStream(data, offset + 16, len - 16))
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read))
					{
						using (MemoryStream memoryStream2 = new MemoryStream(len - 16))
						{
							int num;
							do
							{
								num = cryptoStream.Read(this.readBuffer, 0, 16);
								bool flag = num != 0;
								if (flag)
								{
									memoryStream2.Write(this.readBuffer, 0, num);
								}
							}
							while (num != 0);
							outLen = (int)memoryStream2.Length;
							result = memoryStream2.ToArray();
						}
					}
				}
			}
			return result;
		}

		public bool CheckHMAC(byte[] data, int len)
		{
			this.hmacsha256.ComputeHash(data, 0, len - 32);
			byte[] hash = this.hmacsha256.Hash;
			bool flag = true;
			int num = 0;
			while (num < 4 & flag)
			{
				int num2 = len - 32 + num * 8;
				int num3 = num * 8;
				flag = (data[num2] == hash[num3] && data[num2 + 1] == hash[num3 + 1] && data[num2 + 2] == hash[num3 + 2] && data[num2 + 3] == hash[num3 + 3] && data[num2 + 4] == hash[num3 + 4] && data[num2 + 5] == hash[num3 + 5] && data[num2 + 6] == hash[num3 + 6] && data[num2 + 7] == hash[num3 + 7]);
				num++;
			}
			return flag;
		}
	}
}
