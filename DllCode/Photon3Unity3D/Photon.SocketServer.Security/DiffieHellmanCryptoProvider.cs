using Photon.SocketServer.Numeric;
using System;
using System.Security.Cryptography;

namespace Photon.SocketServer.Security
{
	internal class DiffieHellmanCryptoProvider : ICryptoProvider, IDisposable
	{
		private static readonly BigInteger primeRoot = new BigInteger((long)OakleyGroups.Generator);

		private readonly BigInteger prime;

		private readonly BigInteger secret;

		private readonly BigInteger publicKey;

		private Rijndael crypto;

		private byte[] sharedKey;

		public bool IsInitialized
		{
			get
			{
				return this.crypto != null;
			}
		}

		public byte[] PublicKey
		{
			get
			{
				return this.publicKey.GetBytes();
			}
		}

		public DiffieHellmanCryptoProvider()
		{
			this.prime = new BigInteger(OakleyGroups.OakleyPrime768);
			this.secret = this.GenerateRandomSecret(160);
			this.publicKey = this.CalculatePublicKey();
		}

		public DiffieHellmanCryptoProvider(byte[] sharedSecretHash)
		{
			this.crypto = new RijndaelManaged();
			this.crypto.Key = sharedSecretHash;
			this.crypto.IV = new byte[16];
			this.crypto.Padding = PaddingMode.PKCS7;
		}

		public void DeriveSharedKey(byte[] otherPartyPublicKey)
		{
			BigInteger otherPartyPublicKey2 = new BigInteger(otherPartyPublicKey);
			BigInteger bigInteger = this.CalculateSharedKey(otherPartyPublicKey2);
			this.sharedKey = bigInteger.GetBytes();
			byte[] key;
			using (SHA256 sHA = new SHA256Managed())
			{
				key = sHA.ComputeHash(this.sharedKey);
			}
			this.crypto = new RijndaelManaged();
			this.crypto.Key = key;
			this.crypto.IV = new byte[16];
			this.crypto.Padding = PaddingMode.PKCS7;
		}

		public byte[] Encrypt(byte[] data)
		{
			return this.Encrypt(data, 0, data.Length);
		}

		public byte[] Encrypt(byte[] data, int offset, int count)
		{
			byte[] result;
			using (ICryptoTransform cryptoTransform = this.crypto.CreateEncryptor())
			{
				result = cryptoTransform.TransformFinalBlock(data, offset, count);
			}
			return result;
		}

		public byte[] Decrypt(byte[] data)
		{
			return this.Decrypt(data, 0, data.Length);
		}

		public byte[] Decrypt(byte[] data, int offset, int count)
		{
			byte[] result;
			using (ICryptoTransform cryptoTransform = this.crypto.CreateDecryptor())
			{
				result = cryptoTransform.TransformFinalBlock(data, offset, count);
			}
			return result;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			bool flag = !disposing;
			if (flag)
			{
			}
		}

		private BigInteger CalculatePublicKey()
		{
			return DiffieHellmanCryptoProvider.primeRoot.ModPow(this.secret, this.prime);
		}

		private BigInteger CalculateSharedKey(BigInteger otherPartyPublicKey)
		{
			return otherPartyPublicKey.ModPow(this.secret, this.prime);
		}

		private BigInteger GenerateRandomSecret(int secretLength)
		{
			BigInteger bigInteger;
			do
			{
				bigInteger = BigInteger.GenerateRandom(secretLength);
			}
			while (bigInteger >= this.prime - 1 || bigInteger == 0);
			return bigInteger;
		}
	}
}
