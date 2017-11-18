using Photon.SocketServer.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ExitGames.Client.Photon
{
	public abstract class PeerBase
	{
		internal delegate void MyAction();

		public enum ConnectionStateValue : byte
		{
			Disconnected,
			Connecting,
			Connected = 3,
			Disconnecting,
			AcknowledgingDisconnect,
			Zombie
		}

		internal enum EgMessageType : byte
		{
			Init,
			InitResponse,
			Operation,
			OperationResponse,
			Event,
			InternalOperationRequest = 6,
			InternalOperationResponse,
			Message,
			RawMessage
		}

		internal PhotonPeer ppeer;

		internal IProtocol protocol = Protocol.ProtocolDefault;

		internal ConnectionProtocol usedProtocol;

		internal IPhotonSocket rt;

		internal int ByteCountLastOperation;

		internal int ByteCountCurrentDispatch;

		internal NCommand CommandInCurrentDispatch;

		internal int TrafficPackageHeaderSize;

		internal int packetLossByCrc;

		internal int packetLossByChallenge;

		internal readonly Queue<PeerBase.MyAction> ActionQueue = new Queue<PeerBase.MyAction>();

		internal short peerID = -1;

		internal PeerBase.ConnectionStateValue peerConnectionState;

		internal int serverTimeOffset;

		internal bool serverTimeOffsetIsAvailable;

		internal int roundTripTime;

		internal int roundTripTimeVariance;

		internal int lastRoundTripTime;

		internal int lowestRoundTripTime;

		internal int lastRoundTripTimeVariance;

		internal int highestRoundTripTimeVariance;

		internal int timestampOfLastReceive;

		internal int packetThrottleInterval;

		internal static short peerCount;

		internal long bytesOut;

		internal long bytesIn;

		internal int commandBufferSize = 100;

		internal ICryptoProvider CryptoProvider;

		private readonly Random lagRandomizer = new Random();

		internal readonly LinkedList<SimulationItem> NetSimListOutgoing = new LinkedList<SimulationItem>();

		internal readonly LinkedList<SimulationItem> NetSimListIncoming = new LinkedList<SimulationItem>();

		private readonly NetworkSimulationSet networkSimulationSettings = new NetworkSimulationSet();

		internal Queue<CmdLogItem> CommandLog;

		internal Queue<CmdLogItem> InReliableLog;

		internal object CustomInitData;

		internal string AppId;

		internal int timeBase;

		internal int timeInt;

		internal int timeoutInt;

		internal int timeLastAckReceive;

		internal int timeLastSendAck;

		internal int timeLastSendOutgoing;

		internal const int ENET_PEER_PACKET_LOSS_SCALE = 65536;

		internal const int ENET_PEER_DEFAULT_ROUND_TRIP_TIME = 300;

		internal const int ENET_PEER_PACKET_THROTTLE_INTERVAL = 5000;

		internal bool ApplicationIsInitialized;

		internal bool isEncryptionAvailable;

		internal int outgoingCommandsInStream = 0;

		protected StreamBuffer SerializeMemStream = new StreamBuffer(0);

		internal string ClientVersion
		{
			get
			{
				return this.ppeer.ClientVersion;
			}
		}

		internal Type SocketImplementation
		{
			get
			{
				return this.ppeer.SocketImplementation;
			}
		}

		public string ServerAddress
		{
			get;
			internal set;
		}

		internal string HttpUrlParameters
		{
			get;
			set;
		}

		internal bool TrafficStatsEnabled
		{
			get
			{
				return this.ppeer.TrafficStatsEnabled;
			}
		}

		internal TrafficStats TrafficStatsIncoming
		{
			get
			{
				return this.ppeer.TrafficStatsIncoming;
			}
		}

		internal TrafficStats TrafficStatsOutgoing
		{
			get
			{
				return this.ppeer.TrafficStatsOutgoing;
			}
		}

		internal TrafficStatsGameLevel TrafficStatsGameLevel
		{
			get
			{
				return this.ppeer.TrafficStatsGameLevel;
			}
		}

		internal bool crcEnabled
		{
			get
			{
				return this.ppeer.CrcEnabled;
			}
		}

		internal IPhotonPeerListener Listener
		{
			get
			{
				return this.ppeer.Listener;
			}
		}

		internal DebugLevel debugOut
		{
			get
			{
				return this.ppeer.DebugOut;
			}
		}

		internal int sentCountAllowance
		{
			get
			{
				return this.ppeer.SentCountAllowance;
			}
		}

		internal int DisconnectTimeout
		{
			get
			{
				return this.ppeer.DisconnectTimeout;
			}
		}

		internal int timePingInterval
		{
			get
			{
				return this.ppeer.TimePingInterval;
			}
		}

		internal byte ChannelCount
		{
			get
			{
				return this.ppeer.ChannelCount;
			}
		}

		internal int limitOfUnreliableCommands
		{
			get
			{
				return this.ppeer.LimitOfUnreliableCommands;
			}
		}

		public byte QuickResendAttempts
		{
			get
			{
				return this.ppeer.QuickResendAttempts;
			}
		}

		public NetworkSimulationSet NetworkSimulationSettings
		{
			get
			{
				return this.networkSimulationSettings;
			}
		}

		internal int CommandLogSize
		{
			get
			{
				return this.ppeer.CommandLogSize;
			}
		}

		internal long BytesOut
		{
			get
			{
				return this.bytesOut;
			}
		}

		internal long BytesIn
		{
			get
			{
				return this.bytesIn;
			}
		}

		internal abstract int QueuedIncomingCommandsCount
		{
			get;
		}

		internal abstract int QueuedOutgoingCommandsCount
		{
			get;
		}

		public virtual string PeerID
		{
			get
			{
				return ((ushort)this.peerID).ToString();
			}
		}

		protected internal byte[] TcpConnectionPrefix
		{
			get;
			set;
		}

		protected internal bool IsIpv6
		{
			get
			{
				return this.rt != null && this.rt.AddressResolvedAsIpv6;
			}
		}

		internal static int outgoingStreamBufferSize
		{
			get
			{
				return PhotonPeer.OutgoingStreamBufferSize;
			}
		}

		internal bool IsSendingOnlyAcks
		{
			get
			{
				return this.ppeer.IsSendingOnlyAcks;
			}
		}

		internal int mtu
		{
			get
			{
				return this.ppeer.MaximumTransferUnit;
			}
		}

		internal int rhttpMinConnections
		{
			get
			{
				return this.ppeer.RhttpMinConnections;
			}
		}

		internal int rhttpMaxConnections
		{
			get
			{
				return this.ppeer.RhttpMaxConnections;
			}
		}

		internal void CommandLogResize()
		{
			bool flag = this.CommandLogSize <= 0;
			if (flag)
			{
				this.CommandLog = null;
				this.InReliableLog = null;
			}
			else
			{
				bool flag2 = this.CommandLog == null || this.InReliableLog == null;
				if (flag2)
				{
					this.CommandLogInit();
				}
				while (this.CommandLog.Count > 0 && this.CommandLog.Count > this.CommandLogSize)
				{
					this.CommandLog.Dequeue();
				}
				while (this.InReliableLog.Count > 0 && this.InReliableLog.Count > this.CommandLogSize)
				{
					this.InReliableLog.Dequeue();
				}
			}
		}

		internal void CommandLogInit()
		{
			bool flag = this.CommandLogSize <= 0;
			if (flag)
			{
				this.CommandLog = null;
				this.InReliableLog = null;
			}
			else
			{
				bool flag2 = this.CommandLog == null || this.InReliableLog == null;
				if (flag2)
				{
					this.CommandLog = new Queue<CmdLogItem>(this.CommandLogSize);
					this.InReliableLog = new Queue<CmdLogItem>(this.CommandLogSize);
				}
				else
				{
					this.CommandLog.Clear();
					this.InReliableLog.Clear();
				}
			}
		}

		public string CommandLogToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			int num = (this.usedProtocol != ConnectionProtocol.Udp) ? 0 : ((EnetPeer)this).reliableCommandsRepeated;
			stringBuilder.AppendFormat("PeerId: {0} Now: {1} Server: {2} State: {3} Total Resends: {4} Received {5}ms ago.\n", new object[]
			{
				this.PeerID,
				this.timeInt,
				this.ServerAddress,
				this.peerConnectionState,
				num,
				SupportClass.GetTickCount() - this.timestampOfLastReceive
			});
			bool flag = this.CommandLog == null;
			string result;
			if (flag)
			{
				result = stringBuilder.ToString();
			}
			else
			{
				foreach (CmdLogItem current in this.CommandLog)
				{
					stringBuilder.AppendLine(current.ToString());
				}
				stringBuilder.AppendLine("Received Reliable Log: ");
				foreach (CmdLogItem current2 in this.InReliableLog)
				{
					stringBuilder.AppendLine(current2.ToString());
				}
				result = stringBuilder.ToString();
			}
			return result;
		}

		internal void InitOnce()
		{
			this.networkSimulationSettings.peerBase = this;
		}

		internal abstract bool Connect(string serverAddress, string appID, object customData = null);

		public abstract void OnConnect();

		private string GetHttpKeyValueString(Dictionary<string, string> dic)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (KeyValuePair<string, string> current in dic)
			{
				stringBuilder.Append(current.Key).Append("=").Append(current.Value).Append("&");
			}
			return stringBuilder.ToString();
		}

		[Obsolete("A IPhotonSocket should set AddressResolvedAsIpv6 as soon as the server's address is resolved and before calling OnConnect.")]
		public virtual void SetInitIPV6Bit(bool isV6)
		{
			bool flag = this.debugOut == DebugLevel.ALL;
			if (flag)
			{
				this.Listener.DebugReturn(DebugLevel.ALL, "Setting IPv6 bit " + isV6.ToString());
			}
			this.rt.AddressResolvedAsIpv6 = isV6;
		}

		internal byte[] PrepareConnectData(string serverAddress, string appID, object custom)
		{
			bool flag = this.rt == null || !this.rt.Connected;
			if (flag)
			{
				this.EnqueueDebugReturn(DebugLevel.WARNING, "The peer attempts to prepare an Init-Request but the socket is not connected!?");
			}
			bool flag2 = custom == null;
			byte[] result;
			if (flag2)
			{
				byte[] array = new byte[41];
				byte[] clientVersion = Version.clientVersion;
				array[0] = 243;
				array[1] = 0;
				array[2] = this.protocol.VersionBytes[0];
				array[3] = this.protocol.VersionBytes[1];
				array[4] = this.ppeer.ClientSdkIdShifted;
				array[5] = ((byte)(clientVersion[0] << 4) | clientVersion[1]);
				array[6] = clientVersion[2];
				array[7] = clientVersion[3];
				array[8] = 0;
				bool flag3 = string.IsNullOrEmpty(appID);
				if (flag3)
				{
					appID = "LoadBalancing";
				}
				for (int i = 0; i < 32; i++)
				{
					array[i + 9] = ((i < appID.Length) ? ((byte)appID[i]) : 0);
				}
				bool isIpv = this.IsIpv6;
				if (isIpv)
				{
					byte[] expr_FE_cp_0 = array;
					int expr_FE_cp_1 = 5;
					expr_FE_cp_0[expr_FE_cp_1] |= 128;
				}
				else
				{
					byte[] expr_113_cp_0 = array;
					int expr_113_cp_1 = 5;
					expr_113_cp_0[expr_113_cp_1] &= 127;
				}
				result = array;
			}
			else
			{
				bool flag4 = custom != null;
				if (flag4)
				{
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					dictionary["init"] = null;
					dictionary["app"] = appID;
					dictionary["clientversion"] = this.ClientVersion;
					dictionary["protocol"] = this.protocol.protocolType;
					dictionary["sid"] = this.ppeer.ClientSdkIdShifted.ToString();
					byte[] array2 = null;
					int num = 0;
					bool flag5 = custom != null;
					if (flag5)
					{
						array2 = this.protocol.Serialize(custom);
						num += array2.Length;
					}
					string text = this.GetHttpKeyValueString(dictionary);
					bool isIpv2 = this.IsIpv6;
					if (isIpv2)
					{
						text += "&IPv6";
					}
					string text2 = string.Format("POST /?{0} HTTP/1.1\r\nHost: {1}\r\nContent-Length: {2}\r\n\r\n", text, serverAddress, num);
					byte[] array3 = new byte[text2.Length + num];
					bool flag6 = array2 != null;
					if (flag6)
					{
						Buffer.BlockCopy(array2, 0, array3, text2.Length, array2.Length);
					}
					Buffer.BlockCopy(Encoding.UTF8.GetBytes(text2), 0, array3, 0, text2.Length);
					result = array3;
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		internal string PepareWebSocketUrl(string serverAddress, string appId, object customData)
		{
			StringBuilder stringBuilder = new StringBuilder(1024);
			string empty = string.Empty;
			bool flag = customData != null;
			string result;
			if (flag)
			{
				byte[] array = this.protocol.Serialize(customData);
				bool flag2 = array == null;
				if (flag2)
				{
					this.EnqueueDebugReturn(DebugLevel.ERROR, "Can not deserialize custom data");
					result = null;
					return result;
				}
			}
			stringBuilder.AppendFormat("app={0}&clientver={1}&sid={2}&{3}&initobj={4}", new object[]
			{
				appId,
				this.ClientVersion,
				this.ppeer.ClientSdkIdShifted,
				this.IsIpv6 ? "IPv6" : string.Empty,
				empty
			});
			result = stringBuilder.ToString();
			return result;
		}

		internal abstract void Disconnect();

		internal abstract void StopConnection();

		internal abstract void FetchServerTimestamp();

		internal bool EnqueueOperation(Dictionary<byte, object> parameters, byte opCode, bool sendReliable, byte channelId, bool encrypted)
		{
			return this.EnqueueOperation(parameters, opCode, sendReliable, channelId, encrypted, PeerBase.EgMessageType.Operation);
		}

		internal abstract bool EnqueueOperation(Dictionary<byte, object> parameters, byte opCode, bool sendReliable, byte channelId, bool encrypted, PeerBase.EgMessageType messageType);

		internal abstract bool DispatchIncomingCommands();

		internal abstract bool SendOutgoingCommands();

		internal virtual bool SendAcksOnly()
		{
			return false;
		}

		internal byte[] SerializeMessageToMessage(object message, bool encrypt, byte[] messageHeader, bool writeLength = true)
		{
			StreamBuffer serializeMemStream = this.SerializeMemStream;
			byte[] array3;
			lock (serializeMemStream)
			{
				this.SerializeMemStream.SetLength(0L);
				bool flag = !encrypt;
				if (flag)
				{
					this.SerializeMemStream.Write(messageHeader, 0, messageHeader.Length);
				}
				bool flag2 = message is byte[];
				bool flag3 = flag2;
				if (flag3)
				{
					byte[] array = message as byte[];
					this.SerializeMemStream.Write(array, 0, array.Length);
				}
				else
				{
					this.protocol.SerializeMessage(this.SerializeMemStream, message);
				}
				if (encrypt)
				{
					byte[] array2 = this.CryptoProvider.Encrypt(this.SerializeMemStream.GetBuffer(), 0, (int)this.SerializeMemStream.Length);
					this.SerializeMemStream.SetLength(0L);
					this.SerializeMemStream.Write(messageHeader, 0, messageHeader.Length);
					this.SerializeMemStream.Write(array2, 0, array2.Length);
				}
				array3 = this.SerializeMemStream.ToArray();
			}
			array3[messageHeader.Length - 1] = ((message is byte[]) ? 9 : 8);
			if (encrypt)
			{
				array3[messageHeader.Length - 1] = (array3[messageHeader.Length - 1] | 128);
			}
			if (writeLength)
			{
				int num = 1;
				Protocol.Serialize(array3.Length, array3, ref num);
			}
			return array3;
		}

		internal abstract byte[] SerializeOperationToMessage(byte opCode, Dictionary<byte, object> parameters, PeerBase.EgMessageType messageType, bool encrypt);

		internal abstract void ReceiveIncomingCommands(byte[] inBuff, int dataLength);

		internal void InitCallback()
		{
			bool flag = this.peerConnectionState == PeerBase.ConnectionStateValue.Connecting;
			if (flag)
			{
				this.peerConnectionState = PeerBase.ConnectionStateValue.Connected;
			}
			this.ApplicationIsInitialized = true;
			this.FetchServerTimestamp();
			this.Listener.OnStatusChanged(StatusCode.Connect);
		}

		internal bool ExchangeKeysForEncryption(object lockObject)
		{
			this.isEncryptionAvailable = false;
			bool flag = this.CryptoProvider != null;
			if (flag)
			{
				this.CryptoProvider.Dispose();
				this.CryptoProvider = null;
			}
			bool flag2 = this.CryptoProvider == null;
			if (flag2)
			{
				this.CryptoProvider = new DiffieHellmanCryptoProvider();
			}
			Dictionary<byte, object> dictionary = new Dictionary<byte, object>(1);
			dictionary[PhotonCodes.ClientKey] = this.CryptoProvider.PublicKey;
			bool flag3 = lockObject != null;
			bool result;
			if (flag3)
			{
				lock (lockObject)
				{
					result = this.EnqueueOperation(dictionary, PhotonCodes.InitEncryption, true, 0, false, PeerBase.EgMessageType.InternalOperationRequest);
					return result;
				}
			}
			result = this.EnqueueOperation(dictionary, PhotonCodes.InitEncryption, true, 0, false, PeerBase.EgMessageType.InternalOperationRequest);
			return result;
		}

		internal void DeriveSharedKey(OperationResponse operationResponse)
		{
			bool flag = operationResponse.ReturnCode != 0;
			if (flag)
			{
				this.EnqueueDebugReturn(DebugLevel.ERROR, "Establishing encryption keys failed. " + operationResponse.ToStringFull());
				this.EnqueueStatusCallback(StatusCode.EncryptionFailedToEstablish);
			}
			else
			{
				byte[] array = (byte[])operationResponse[PhotonCodes.ServerKey];
				bool flag2 = array == null || array.Length == 0;
				if (flag2)
				{
					this.EnqueueDebugReturn(DebugLevel.ERROR, "Establishing encryption keys failed. Server's public key is null or empty. " + operationResponse.ToStringFull());
					this.EnqueueStatusCallback(StatusCode.EncryptionFailedToEstablish);
				}
				else
				{
					this.CryptoProvider.DeriveSharedKey(array);
					this.isEncryptionAvailable = true;
					this.EnqueueStatusCallback(StatusCode.EncryptionEstablished);
				}
			}
		}

		internal void EnqueueActionForDispatch(PeerBase.MyAction action)
		{
			Queue<PeerBase.MyAction> actionQueue = this.ActionQueue;
			lock (actionQueue)
			{
				this.ActionQueue.Enqueue(action);
			}
		}

		internal void EnqueueDebugReturn(DebugLevel level, string debugReturn)
		{
			Queue<PeerBase.MyAction> actionQueue = this.ActionQueue;
			lock (actionQueue)
			{
				this.ActionQueue.Enqueue(delegate
				{
					this.Listener.DebugReturn(level, debugReturn);
				});
			}
		}

		internal void EnqueueStatusCallback(StatusCode statusValue)
		{
			Queue<PeerBase.MyAction> actionQueue = this.ActionQueue;
			lock (actionQueue)
			{
				this.ActionQueue.Enqueue(delegate
				{
					this.Listener.OnStatusChanged(statusValue);
				});
			}
		}

		internal virtual void InitPeerBase()
		{
			this.ppeer.InitializeTrafficStats();
			this.ByteCountLastOperation = 0;
			this.ByteCountCurrentDispatch = 0;
			this.bytesIn = 0L;
			this.bytesOut = 0L;
			this.packetLossByCrc = 0;
			this.packetLossByChallenge = 0;
			this.networkSimulationSettings.LostPackagesIn = 0;
			this.networkSimulationSettings.LostPackagesOut = 0;
			LinkedList<SimulationItem> netSimListOutgoing = this.NetSimListOutgoing;
			lock (netSimListOutgoing)
			{
				this.NetSimListOutgoing.Clear();
			}
			LinkedList<SimulationItem> netSimListIncoming = this.NetSimListIncoming;
			lock (netSimListIncoming)
			{
				this.NetSimListIncoming.Clear();
			}
			this.peerConnectionState = PeerBase.ConnectionStateValue.Disconnected;
			this.timeBase = SupportClass.GetTickCount();
			this.isEncryptionAvailable = false;
			this.ApplicationIsInitialized = false;
			this.roundTripTime = 300;
			this.roundTripTimeVariance = 0;
			this.packetThrottleInterval = 5000;
			this.serverTimeOffsetIsAvailable = false;
			this.serverTimeOffset = 0;
		}

		internal virtual bool DeserializeMessageAndCallback(byte[] inBuff)
		{
			bool flag = inBuff.Length < 2;
			bool result;
			if (flag)
			{
				bool flag2 = this.debugOut >= DebugLevel.ERROR;
				if (flag2)
				{
					this.Listener.DebugReturn(DebugLevel.ERROR, "Incoming UDP data too short! " + inBuff.Length);
				}
				result = false;
			}
			else
			{
				bool flag3 = inBuff[0] != 243 && inBuff[0] != 253;
				if (flag3)
				{
					bool flag4 = this.debugOut >= DebugLevel.ERROR;
					if (flag4)
					{
						this.Listener.DebugReturn(DebugLevel.ALL, "No regular operation UDP message: " + inBuff[0]);
					}
					result = false;
				}
				else
				{
					byte b = inBuff[1] & 127;
					bool flag5 = (inBuff[1] & 128) > 0;
					StreamBuffer streamBuffer = null;
					bool flag6 = b != 1;
					if (flag6)
					{
						try
						{
							bool flag7 = flag5;
							if (flag7)
							{
								inBuff = this.CryptoProvider.Decrypt(inBuff, 2, inBuff.Length - 2);
								streamBuffer = new StreamBuffer(inBuff);
							}
							else
							{
								streamBuffer = new StreamBuffer(inBuff);
								streamBuffer.Seek(2L, SeekOrigin.Begin);
							}
						}
						catch (Exception ex)
						{
							bool flag8 = this.debugOut >= DebugLevel.ERROR;
							if (flag8)
							{
								this.Listener.DebugReturn(DebugLevel.ERROR, ex.ToString());
							}
							SupportClass.WriteStackTrace(ex);
							result = false;
							return result;
						}
					}
					int num = 0;
					switch (b)
					{
					case 1:
						this.InitCallback();
						goto IL_360;
					case 3:
					{
						OperationResponse operationResponse = this.protocol.DeserializeOperationResponse(streamBuffer);
						bool trafficStatsEnabled = this.TrafficStatsEnabled;
						if (trafficStatsEnabled)
						{
							this.TrafficStatsGameLevel.CountResult(this.ByteCountCurrentDispatch);
							num = SupportClass.GetTickCount();
						}
						this.Listener.OnOperationResponse(operationResponse);
						bool trafficStatsEnabled2 = this.TrafficStatsEnabled;
						if (trafficStatsEnabled2)
						{
							this.TrafficStatsGameLevel.TimeForResponseCallback(operationResponse.OperationCode, SupportClass.GetTickCount() - num);
						}
						goto IL_360;
					}
					case 4:
					{
						EventData eventData = this.protocol.DeserializeEventData(streamBuffer);
						bool trafficStatsEnabled3 = this.TrafficStatsEnabled;
						if (trafficStatsEnabled3)
						{
							this.TrafficStatsGameLevel.CountEvent(this.ByteCountCurrentDispatch);
							num = SupportClass.GetTickCount();
						}
						this.Listener.OnEvent(eventData);
						bool trafficStatsEnabled4 = this.TrafficStatsEnabled;
						if (trafficStatsEnabled4)
						{
							this.TrafficStatsGameLevel.TimeForEventCallback(eventData.Code, SupportClass.GetTickCount() - num);
						}
						goto IL_360;
					}
					case 7:
					{
						OperationResponse operationResponse = this.protocol.DeserializeOperationResponse(streamBuffer);
						bool trafficStatsEnabled5 = this.TrafficStatsEnabled;
						if (trafficStatsEnabled5)
						{
							this.TrafficStatsGameLevel.CountResult(this.ByteCountCurrentDispatch);
							num = SupportClass.GetTickCount();
						}
						bool flag9 = operationResponse.OperationCode == PhotonCodes.InitEncryption;
						if (flag9)
						{
							this.DeriveSharedKey(operationResponse);
						}
						else
						{
							bool flag10 = operationResponse.OperationCode == PhotonCodes.Ping;
							if (flag10)
							{
								TPeer tPeer = this as TPeer;
								bool flag11 = tPeer != null;
								if (flag11)
								{
									tPeer.ReadPingResult(operationResponse);
								}
								else
								{
									this.EnqueueDebugReturn(DebugLevel.ERROR, "Ping response not used. " + operationResponse.ToStringFull());
								}
							}
							else
							{
								this.EnqueueDebugReturn(DebugLevel.ERROR, "Received unknown internal operation. " + operationResponse.ToStringFull());
							}
						}
						bool trafficStatsEnabled6 = this.TrafficStatsEnabled;
						if (trafficStatsEnabled6)
						{
							this.TrafficStatsGameLevel.TimeForResponseCallback(operationResponse.OperationCode, SupportClass.GetTickCount() - num);
						}
						goto IL_360;
					}
					}
					this.EnqueueDebugReturn(DebugLevel.ERROR, "unexpected msgType " + b);
					IL_360:
					result = true;
				}
			}
			return result;
		}

		internal void SendNetworkSimulated(PeerBase.MyAction sendAction)
		{
			bool flag = !this.NetworkSimulationSettings.IsSimulationEnabled;
			if (flag)
			{
				sendAction();
			}
			else
			{
				bool flag2 = this.usedProtocol == ConnectionProtocol.Udp && this.NetworkSimulationSettings.OutgoingLossPercentage > 0 && this.lagRandomizer.Next(101) < this.NetworkSimulationSettings.OutgoingLossPercentage;
				if (flag2)
				{
					NetworkSimulationSet expr_62 = this.networkSimulationSettings;
					int lostPackagesOut = expr_62.LostPackagesOut;
					expr_62.LostPackagesOut = lostPackagesOut + 1;
				}
				else
				{
					int num = (this.networkSimulationSettings.OutgoingJitter <= 0) ? 0 : (this.lagRandomizer.Next(this.networkSimulationSettings.OutgoingJitter * 2) - this.networkSimulationSettings.OutgoingJitter);
					int num2 = this.networkSimulationSettings.OutgoingLag + num;
					int num3 = SupportClass.GetTickCount() + num2;
					SimulationItem value = new SimulationItem
					{
						ActionToExecute = sendAction,
						TimeToExecute = num3,
						Delay = num2
					};
					LinkedList<SimulationItem> netSimListOutgoing = this.NetSimListOutgoing;
					lock (netSimListOutgoing)
					{
						bool flag3 = this.NetSimListOutgoing.Count == 0 || this.usedProtocol == ConnectionProtocol.Tcp;
						if (flag3)
						{
							this.NetSimListOutgoing.AddLast(value);
						}
						else
						{
							LinkedListNode<SimulationItem> linkedListNode = this.NetSimListOutgoing.First;
							while (linkedListNode != null && linkedListNode.Value.TimeToExecute < num3)
							{
								linkedListNode = linkedListNode.Next;
							}
							bool flag4 = linkedListNode == null;
							if (flag4)
							{
								this.NetSimListOutgoing.AddLast(value);
							}
							else
							{
								this.NetSimListOutgoing.AddBefore(linkedListNode, value);
							}
						}
					}
				}
			}
		}

		internal void ReceiveNetworkSimulated(PeerBase.MyAction receiveAction)
		{
			bool flag = !this.networkSimulationSettings.IsSimulationEnabled;
			if (flag)
			{
				receiveAction();
			}
			else
			{
				bool flag2 = this.usedProtocol == ConnectionProtocol.Udp && this.networkSimulationSettings.IncomingLossPercentage > 0 && this.lagRandomizer.Next(101) < this.networkSimulationSettings.IncomingLossPercentage;
				if (flag2)
				{
					NetworkSimulationSet expr_62 = this.networkSimulationSettings;
					int lostPackagesIn = expr_62.LostPackagesIn;
					expr_62.LostPackagesIn = lostPackagesIn + 1;
				}
				else
				{
					int num = (this.networkSimulationSettings.IncomingJitter <= 0) ? 0 : (this.lagRandomizer.Next(this.networkSimulationSettings.IncomingJitter * 2) - this.networkSimulationSettings.IncomingJitter);
					int num2 = this.networkSimulationSettings.IncomingLag + num;
					int num3 = SupportClass.GetTickCount() + num2;
					SimulationItem value = new SimulationItem
					{
						ActionToExecute = receiveAction,
						TimeToExecute = num3,
						Delay = num2
					};
					LinkedList<SimulationItem> netSimListIncoming = this.NetSimListIncoming;
					lock (netSimListIncoming)
					{
						bool flag3 = this.NetSimListIncoming.Count == 0 || this.usedProtocol == ConnectionProtocol.Tcp;
						if (flag3)
						{
							this.NetSimListIncoming.AddLast(value);
						}
						else
						{
							LinkedListNode<SimulationItem> linkedListNode = this.NetSimListIncoming.First;
							while (linkedListNode != null && linkedListNode.Value.TimeToExecute < num3)
							{
								linkedListNode = linkedListNode.Next;
							}
							bool flag4 = linkedListNode == null;
							if (flag4)
							{
								this.NetSimListIncoming.AddLast(value);
							}
							else
							{
								this.NetSimListIncoming.AddBefore(linkedListNode, value);
							}
						}
					}
				}
			}
		}

		protected internal void NetworkSimRun()
		{
			while (true)
			{
				bool flag = false;
				ManualResetEvent netSimManualResetEvent = this.networkSimulationSettings.NetSimManualResetEvent;
				lock (netSimManualResetEvent)
				{
					flag = this.networkSimulationSettings.IsSimulationEnabled;
				}
				bool flag2 = !flag;
				if (flag2)
				{
					this.networkSimulationSettings.NetSimManualResetEvent.WaitOne();
				}
				else
				{
					LinkedList<SimulationItem> netSimListIncoming = this.NetSimListIncoming;
					lock (netSimListIncoming)
					{
						while (this.NetSimListIncoming.First != null)
						{
							SimulationItem value = this.NetSimListIncoming.First.Value;
							bool flag3 = value.stopw.ElapsedMilliseconds < (long)value.Delay;
							if (flag3)
							{
								break;
							}
							value.ActionToExecute();
							this.NetSimListIncoming.RemoveFirst();
						}
					}
					LinkedList<SimulationItem> netSimListOutgoing = this.NetSimListOutgoing;
					lock (netSimListOutgoing)
					{
						while (this.NetSimListOutgoing.First != null)
						{
							SimulationItem value2 = this.NetSimListOutgoing.First.Value;
							bool flag4 = value2.stopw.ElapsedMilliseconds < (long)value2.Delay;
							if (flag4)
							{
								break;
							}
							value2.ActionToExecute();
							this.NetSimListOutgoing.RemoveFirst();
						}
					}
					Thread.Sleep(0);
				}
			}
		}

		internal void UpdateRoundTripTimeAndVariance(int lastRoundtripTime)
		{
			bool flag = lastRoundtripTime < 0;
			if (!flag)
			{
				this.roundTripTimeVariance -= this.roundTripTimeVariance / 4;
				bool flag2 = lastRoundtripTime >= this.roundTripTime;
				if (flag2)
				{
					this.roundTripTime += (lastRoundtripTime - this.roundTripTime) / 8;
					this.roundTripTimeVariance += (lastRoundtripTime - this.roundTripTime) / 4;
				}
				else
				{
					this.roundTripTime += (lastRoundtripTime - this.roundTripTime) / 8;
					this.roundTripTimeVariance -= (lastRoundtripTime - this.roundTripTime) / 4;
				}
				bool flag3 = this.roundTripTime < this.lowestRoundTripTime;
				if (flag3)
				{
					this.lowestRoundTripTime = this.roundTripTime;
				}
				bool flag4 = this.roundTripTimeVariance > this.highestRoundTripTimeVariance;
				if (flag4)
				{
					this.highestRoundTripTimeVariance = this.roundTripTimeVariance;
				}
			}
		}

		internal virtual bool InitUdpEncryption(byte[] encryptionSecret, byte[] hmacSecret)
		{
			return false;
		}

		internal virtual void InitEncryption(byte[] secret)
		{
			this.CryptoProvider = new DiffieHellmanCryptoProvider(secret);
			this.isEncryptionAvailable = true;
		}
	}
}
