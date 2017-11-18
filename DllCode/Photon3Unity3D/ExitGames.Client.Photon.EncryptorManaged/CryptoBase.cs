using System;
using System.Security.Cryptography;

namespace ExitGames.Client.Photon.EncryptorManaged
{
	public class CryptoBase : IDisposable
	{
		public const int BLOCK_SIZE = 16;

		public const int IV_SIZE = 16;

		public const int HMAC_SIZE = 32;

		protected Aes encryptor;

		protected HMACSHA256 hmacsha256;

		~CryptoBase()
		{
			this.Dispose(false);
		}

		public void Init(byte[] encryptionSecret, byte[] hmacSecret)
		{
			this.encryptor = new AesManaged
			{
				Key = encryptionSecret
			};
			this.encryptor.GenerateIV();
			this.hmacsha256 = new HMACSHA256(hmacSecret);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool dispose)
		{
			bool flag = this.encryptor != null;
			if (flag)
			{
				this.encryptor.Clear();
				this.encryptor = null;
			}
			bool flag2 = this.hmacsha256 != null;
			if (flag2)
			{
				this.hmacsha256.Clear();
				this.hmacsha256 = null;
			}
		}
	}
}
