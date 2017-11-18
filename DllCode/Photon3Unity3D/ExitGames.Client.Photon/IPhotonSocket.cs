using System;
using System.Net;
using System.Net.Sockets;

namespace ExitGames.Client.Photon
{
	public abstract class IPhotonSocket
	{
		protected internal PeerBase peerBase;

		public bool PollReceive;

		protected IPhotonPeerListener Listener
		{
			get
			{
				return this.peerBase.Listener;
			}
		}

		public ConnectionProtocol Protocol
		{
			get;
			protected set;
		}

		public PhotonSocketState State
		{
			get;
			protected set;
		}

		public string ServerAddress
		{
			get;
			protected set;
		}

		public int ServerPort
		{
			get;
			protected set;
		}

		public bool AddressResolvedAsIpv6
		{
			get;
			protected internal set;
		}

		public string UrlProtocol
		{
			get;
			protected set;
		}

		public string UrlPath
		{
			get;
			protected set;
		}

		public bool Connected
		{
			get
			{
				return this.State == PhotonSocketState.Connected;
			}
		}

		protected internal int MTU
		{
			get
			{
				return this.peerBase.mtu;
			}
		}

		public IPhotonSocket(PeerBase peerBase)
		{
			bool flag = peerBase == null;
			if (flag)
			{
				throw new Exception("Can't init without peer");
			}
			this.peerBase = peerBase;
		}

		public virtual bool Connect()
		{
			bool flag = this.State > PhotonSocketState.Disconnected;
			bool result;
			if (flag)
			{
				bool flag2 = this.peerBase.debugOut >= DebugLevel.ERROR;
				if (flag2)
				{
					this.peerBase.Listener.DebugReturn(DebugLevel.ERROR, "Connect() failed: connection in State: " + this.State);
				}
				result = false;
			}
			else
			{
				bool flag3 = this.peerBase == null || this.Protocol != this.peerBase.usedProtocol;
				if (flag3)
				{
					result = false;
				}
				else
				{
					string serverAddress;
					ushort serverPort;
					string urlProtocol;
					string urlPath;
					bool flag4 = !this.TryParseAddress(this.peerBase.ServerAddress, out serverAddress, out serverPort, out urlProtocol, out urlPath);
					if (flag4)
					{
						bool flag5 = this.peerBase.debugOut >= DebugLevel.ERROR;
						if (flag5)
						{
							this.peerBase.Listener.DebugReturn(DebugLevel.ERROR, "Failed parsing address: " + this.peerBase.ServerAddress);
						}
						result = false;
					}
					else
					{
						this.ServerAddress = serverAddress;
						this.ServerPort = (int)serverPort;
						this.UrlProtocol = urlProtocol;
						this.UrlPath = urlPath;
						bool flag6 = this.peerBase.debugOut >= DebugLevel.ALL;
						if (flag6)
						{
							this.Listener.DebugReturn(DebugLevel.ALL, string.Concat(new object[]
							{
								"IPhotonSocket.Connect() ",
								this.ServerAddress,
								":",
								this.ServerPort,
								" this.Protocol: ",
								this.Protocol
							}));
						}
						result = true;
					}
				}
			}
			return result;
		}

		public abstract bool Disconnect();

		public abstract PhotonSocketError Send(byte[] data, int length);

		public abstract PhotonSocketError Receive(out byte[] data);

		public void HandleReceivedDatagram(byte[] inBuffer, int length, bool willBeReused)
		{
			bool isSimulationEnabled = this.peerBase.NetworkSimulationSettings.IsSimulationEnabled;
			if (isSimulationEnabled)
			{
				if (willBeReused)
				{
					byte[] inBufferCopy = new byte[length];
					Buffer.BlockCopy(inBuffer, 0, inBufferCopy, 0, length);
					this.peerBase.ReceiveNetworkSimulated(delegate
					{
						this.peerBase.ReceiveIncomingCommands(inBufferCopy, length);
					});
				}
				else
				{
					this.peerBase.ReceiveNetworkSimulated(delegate
					{
						this.peerBase.ReceiveIncomingCommands(inBuffer, length);
					});
				}
			}
			else
			{
				this.peerBase.ReceiveIncomingCommands(inBuffer, length);
			}
		}

		public bool ReportDebugOfLevel(DebugLevel levelOfMessage)
		{
			return this.peerBase.debugOut >= levelOfMessage;
		}

		public void EnqueueDebugReturn(DebugLevel debugLevel, string message)
		{
			this.peerBase.EnqueueDebugReturn(debugLevel, message);
		}

		protected internal void HandleException(StatusCode statusCode)
		{
			this.State = PhotonSocketState.Disconnecting;
			this.peerBase.EnqueueStatusCallback(statusCode);
			this.peerBase.EnqueueActionForDispatch(delegate
			{
				this.peerBase.Disconnect();
			});
		}

		protected internal bool TryParseAddress(string url, out string address, out ushort port, out string urlProtocol, out string urlPath)
		{
			address = string.Empty;
			port = 0;
			urlProtocol = string.Empty;
			urlPath = string.Empty;
			string text = url;
			bool flag = string.IsNullOrEmpty(text);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				int num = text.IndexOf("://");
				bool flag2 = num >= 0;
				if (flag2)
				{
					urlProtocol = text.Substring(0, num);
					text = text.Substring(num + 3);
				}
				num = text.IndexOf("/");
				bool flag3 = num >= 0;
				if (flag3)
				{
					urlPath = text.Substring(num);
					text = text.Substring(0, num);
				}
				num = text.LastIndexOf(':');
				bool flag4 = num < 0;
				if (flag4)
				{
					result = false;
				}
				else
				{
					bool flag5 = text.IndexOf(':') != num && (!text.Contains("[") || !text.Contains("]"));
					if (flag5)
					{
						result = false;
					}
					else
					{
						address = text.Substring(0, num);
						string s = text.Substring(num + 1);
						bool flag6 = ushort.TryParse(s, out port);
						result = flag6;
					}
				}
			}
			return result;
		}

		protected internal bool IsIpv6SimpleCheck(IPAddress address)
		{
			return address != null && address.ToString().Contains(":");
		}

		protected internal static IPAddress GetIpAddress(string address)
		{
			IPAddress iPAddress = null;
			bool flag = IPAddress.TryParse(address, out iPAddress);
			IPAddress result;
			if (flag)
			{
				result = iPAddress;
			}
			else
			{
				IPHostEntry hostEntry = Dns.GetHostEntry(address);
				IPAddress[] addressList = hostEntry.AddressList;
				IPAddress[] array = addressList;
				for (int i = 0; i < array.Length; i++)
				{
					IPAddress iPAddress2 = array[i];
					bool flag2 = iPAddress2.AddressFamily == AddressFamily.InterNetworkV6;
					if (flag2)
					{
						result = iPAddress2;
						return result;
					}
					bool flag3 = iPAddress == null && iPAddress2.AddressFamily == AddressFamily.InterNetwork;
					if (flag3)
					{
						iPAddress = iPAddress2;
					}
				}
				result = iPAddress;
			}
			return result;
		}
	}
}
