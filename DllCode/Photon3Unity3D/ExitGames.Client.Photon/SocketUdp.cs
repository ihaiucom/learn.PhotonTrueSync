using System;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace ExitGames.Client.Photon
{
	internal class SocketUdp : IPhotonSocket, IDisposable
	{
		private Socket sock;

		private readonly object syncer = new object();

		public SocketUdp(PeerBase npeer) : base(npeer)
		{
			bool flag = base.ReportDebugOfLevel(DebugLevel.ALL);
			if (flag)
			{
				base.Listener.DebugReturn(DebugLevel.ALL, "CSharpSocket: UDP, Unity3d.");
			}
			base.Protocol = ConnectionProtocol.Udp;
			this.PollReceive = false;
		}

		public void Dispose()
		{
			base.State = PhotonSocketState.Disconnecting;
			bool flag = this.sock != null;
			if (flag)
			{
				try
				{
					bool connected = this.sock.Connected;
					if (connected)
					{
						this.sock.Close();
					}
				}
				catch (Exception arg)
				{
					base.EnqueueDebugReturn(DebugLevel.INFO, "Exception in Dispose(): " + arg);
				}
			}
			this.sock = null;
			base.State = PhotonSocketState.Disconnected;
		}

		public override bool Connect()
		{
			object obj = this.syncer;
			bool result;
			lock (obj)
			{
				bool flag = base.Connect();
				bool flag2 = !flag;
				if (flag2)
				{
					result = false;
				}
				else
				{
					base.State = PhotonSocketState.Connecting;
					new Thread(new ThreadStart(this.DnsAndConnect))
					{
						Name = "photon dns thread",
						IsBackground = true
					}.Start();
					result = true;
				}
			}
			return result;
		}

		public override bool Disconnect()
		{
			bool flag = base.ReportDebugOfLevel(DebugLevel.INFO);
			if (flag)
			{
				base.EnqueueDebugReturn(DebugLevel.INFO, "CSharpSocket.Disconnect()");
			}
			base.State = PhotonSocketState.Disconnecting;
			object obj = this.syncer;
			lock (obj)
			{
				bool flag2 = this.sock != null;
				if (flag2)
				{
					try
					{
						this.sock.Close();
					}
					catch (Exception arg)
					{
						base.EnqueueDebugReturn(DebugLevel.INFO, "Exception in Disconnect(): " + arg);
					}
					this.sock = null;
				}
			}
			base.State = PhotonSocketState.Disconnected;
			return true;
		}

		public override PhotonSocketError Send(byte[] data, int length)
		{
			object obj = this.syncer;
			PhotonSocketError result;
			lock (obj)
			{
				bool flag = this.sock == null || !this.sock.Connected;
				if (flag)
				{
					result = PhotonSocketError.Skipped;
					return result;
				}
				try
				{
					this.sock.Send(data, 0, length, SocketFlags.None);
				}
				catch (Exception ex)
				{
					bool flag2 = base.ReportDebugOfLevel(DebugLevel.ERROR);
					if (flag2)
					{
						base.EnqueueDebugReturn(DebugLevel.ERROR, "Cannot send to: " + base.ServerAddress + ". " + ex.Message);
					}
					result = PhotonSocketError.Exception;
					return result;
				}
			}
			result = PhotonSocketError.Success;
			return result;
		}

		public override PhotonSocketError Receive(out byte[] data)
		{
			data = null;
			return PhotonSocketError.NoData;
		}

		internal void DnsAndConnect()
		{
			IPAddress iPAddress = null;
			try
			{
				iPAddress = IPhotonSocket.GetIpAddress(base.ServerAddress);
				bool flag = iPAddress == null;
				if (flag)
				{
					throw new ArgumentException("Invalid IPAddress. Address: " + base.ServerAddress);
				}
				object obj = this.syncer;
				lock (obj)
				{
					bool flag2 = base.State == PhotonSocketState.Disconnecting || base.State == PhotonSocketState.Disconnected;
					if (flag2)
					{
						return;
					}
					this.sock = new Socket(iPAddress.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
					this.sock.Connect(iPAddress, base.ServerPort);
					base.AddressResolvedAsIpv6 = base.IsIpv6SimpleCheck(iPAddress);
					base.State = PhotonSocketState.Connected;
					this.peerBase.OnConnect();
				}
			}
			catch (SecurityException ex)
			{
				bool flag3 = base.ReportDebugOfLevel(DebugLevel.ERROR);
				if (flag3)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, string.Concat(new string[]
					{
						"Connect() to '",
						base.ServerAddress,
						"' (",
						(iPAddress == null) ? "" : iPAddress.AddressFamily.ToString(),
						") failed: ",
						ex.ToString()
					}));
				}
				base.HandleException(StatusCode.SecurityExceptionOnConnect);
				return;
			}
			catch (Exception ex2)
			{
				bool flag4 = base.ReportDebugOfLevel(DebugLevel.ERROR);
				if (flag4)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, string.Concat(new string[]
					{
						"Connect() to '",
						base.ServerAddress,
						"' (",
						(iPAddress == null) ? "" : iPAddress.AddressFamily.ToString(),
						") failed: ",
						ex2.ToString()
					}));
				}
				base.HandleException(StatusCode.ExceptionOnConnect);
				return;
			}
			new Thread(new ThreadStart(this.ReceiveLoop))
			{
				Name = "photon receive thread",
				IsBackground = true
			}.Start();
		}

		public void ReceiveLoop()
		{
			byte[] array = new byte[base.MTU];
			while (base.State == PhotonSocketState.Connected)
			{
				try
				{
					int length = this.sock.Receive(array);
					base.HandleReceivedDatagram(array, length, true);
				}
				catch (Exception ex)
				{
					bool flag = base.State != PhotonSocketState.Disconnecting && base.State > PhotonSocketState.Disconnected;
					if (flag)
					{
						bool flag2 = base.ReportDebugOfLevel(DebugLevel.ERROR);
						if (flag2)
						{
							base.EnqueueDebugReturn(DebugLevel.ERROR, string.Concat(new object[]
							{
								"Receive issue. State: ",
								base.State,
								". Server: '",
								base.ServerAddress,
								"' Exception: ",
								ex
							}));
						}
						base.HandleException(StatusCode.ExceptionOnReceive);
					}
				}
			}
			this.Disconnect();
		}
	}
}
