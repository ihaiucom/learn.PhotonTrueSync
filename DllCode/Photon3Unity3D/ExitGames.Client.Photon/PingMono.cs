using System;
using System.Net.Sockets;

namespace ExitGames.Client.Photon
{
	public class PingMono : PhotonPing
	{
		private Socket sock;

		public override bool StartPing(string ip)
		{
			base.Init();
			try
			{
				bool flag = ip.Contains(".");
				if (flag)
				{
					this.sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				}
				else
				{
					this.sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
				}
				this.sock.ReceiveTimeout = 5000;
				this.sock.Connect(ip, 5055);
				this.PingBytes[this.PingBytes.Length - 1] = this.PingId;
				this.sock.Send(this.PingBytes);
				this.PingBytes[this.PingBytes.Length - 1] = this.PingId - 1;
			}
			catch (Exception value)
			{
				this.sock = null;
				Console.WriteLine(value);
			}
			return false;
		}

		public override bool Done()
		{
			bool flag = this.GotResult || this.sock == null;
			bool result;
			if (flag)
			{
				result = true;
			}
			else
			{
				bool flag2 = this.sock.Available <= 0;
				if (flag2)
				{
					result = false;
				}
				else
				{
					int num = this.sock.Receive(this.PingBytes, SocketFlags.None);
					bool flag3 = this.PingBytes[this.PingBytes.Length - 1] == this.PingId && num == this.PingLength;
					bool flag4 = !flag3;
					if (flag4)
					{
						this.DebugString += " ReplyMatch is false! ";
					}
					this.Successful = (num == this.PingBytes.Length && this.PingBytes[this.PingBytes.Length - 1] == this.PingId);
					this.GotResult = true;
					result = true;
				}
			}
			return result;
		}

		public override void Dispose()
		{
			try
			{
				this.sock.Close();
			}
			catch
			{
			}
			this.sock = null;
		}
	}
}
