using System;
using System.Collections.Generic;
using System.IO;

namespace ExitGames.Client.Photon
{
	internal class TPeer : PeerBase
	{
		internal const int TCP_HEADER_BYTES = 7;

		internal const int MSG_HEADER_BYTES = 2;

		public const int ALL_HEADER_BYTES = 9;

		private Queue<byte[]> incomingList = new Queue<byte[]>(32);

		internal List<byte[]> outgoingStream;

		private int lastPingResult;

		private byte[] pingRequest;

		internal static readonly byte[] tcpFramedMessageHead = new byte[]
		{
			251,
			0,
			0,
			0,
			0,
			0,
			0,
			243,
			2
		};

		internal static readonly byte[] tcpMsgHead = new byte[]
		{
			243,
			2
		};

		internal byte[] messageHeader;

		protected internal bool DoFraming;

		internal override int QueuedIncomingCommandsCount
		{
			get
			{
				return this.incomingList.Count;
			}
		}

		internal override int QueuedOutgoingCommandsCount
		{
			get
			{
				return this.outgoingCommandsInStream;
			}
		}

		internal TPeer()
		{
			byte[] expr_14 = new byte[5];
			expr_14[0] = 240;
			this.pingRequest = expr_14;
			this.DoFraming = true;
			base..ctor();
			PeerBase.peerCount += 1;
			base.InitOnce();
			this.TrafficPackageHeaderSize = 0;
		}

		internal override void InitPeerBase()
		{
			base.InitPeerBase();
			this.incomingList = new Queue<byte[]>(32);
		}

		internal override bool Connect(string serverAddress, string appID, object customData = null)
		{
			bool flag = this.peerConnectionState > PeerBase.ConnectionStateValue.Disconnected;
			bool result;
			if (flag)
			{
				base.Listener.DebugReturn(DebugLevel.WARNING, "Connect() can't be called if peer is not Disconnected. Not connecting.");
				result = false;
			}
			else
			{
				bool flag2 = base.debugOut >= DebugLevel.ALL;
				if (flag2)
				{
					base.Listener.DebugReturn(DebugLevel.ALL, "Connect()");
				}
				base.ServerAddress = serverAddress;
				this.InitPeerBase();
				this.outgoingStream = new List<byte[]>();
				bool flag3 = this.usedProtocol == ConnectionProtocol.WebSocket || this.usedProtocol == ConnectionProtocol.WebSocketSecure;
				if (flag3)
				{
					serverAddress = base.PepareWebSocketUrl(serverAddress, appID, customData);
				}
				bool flag4 = base.SocketImplementation != null;
				if (flag4)
				{
					this.rt = (IPhotonSocket)Activator.CreateInstance(base.SocketImplementation, new object[]
					{
						this
					});
				}
				else
				{
					this.rt = new SocketTcp(this);
				}
				bool flag5 = this.rt == null;
				if (flag5)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, "Connect() failed, because SocketImplementation or socket was null. Set PhotonPeer.SocketImplementation before Connect(). SocketImplementation: " + base.SocketImplementation);
					result = false;
				}
				else
				{
					this.messageHeader = (this.DoFraming ? TPeer.tcpFramedMessageHead : TPeer.tcpMsgHead);
					bool flag6 = this.rt.Connect();
					if (flag6)
					{
						this.peerConnectionState = PeerBase.ConnectionStateValue.Connecting;
						result = true;
					}
					else
					{
						this.peerConnectionState = PeerBase.ConnectionStateValue.Disconnected;
						result = false;
					}
				}
			}
			return result;
		}

		public override void OnConnect()
		{
			byte[] data = base.PrepareConnectData(base.ServerAddress, this.AppId, this.CustomInitData);
			this.EnqueueInit(data);
			this.SendOutgoingCommands();
		}

		internal override void Disconnect()
		{
			bool flag = this.peerConnectionState == PeerBase.ConnectionStateValue.Disconnected || this.peerConnectionState == PeerBase.ConnectionStateValue.Disconnecting;
			if (!flag)
			{
				bool flag2 = base.debugOut >= DebugLevel.ALL;
				if (flag2)
				{
					base.Listener.DebugReturn(DebugLevel.ALL, "TPeer.Disconnect()");
				}
				this.StopConnection();
			}
		}

		internal override void StopConnection()
		{
			this.peerConnectionState = PeerBase.ConnectionStateValue.Disconnecting;
			bool flag = this.rt != null;
			if (flag)
			{
				this.rt.Disconnect();
			}
			Queue<byte[]> obj = this.incomingList;
			lock (obj)
			{
				this.incomingList.Clear();
			}
			this.peerConnectionState = PeerBase.ConnectionStateValue.Disconnected;
			base.EnqueueStatusCallback(StatusCode.Disconnect);
		}

		internal override void FetchServerTimestamp()
		{
			bool flag = this.peerConnectionState != PeerBase.ConnectionStateValue.Connected;
			if (flag)
			{
				bool flag2 = base.debugOut >= DebugLevel.INFO;
				if (flag2)
				{
					base.Listener.DebugReturn(DebugLevel.INFO, "FetchServerTimestamp() was skipped, as the client is not connected. Current ConnectionState: " + this.peerConnectionState);
				}
				base.Listener.OnStatusChanged(StatusCode.SendError);
			}
			else
			{
				this.SendPing();
				this.serverTimeOffsetIsAvailable = false;
			}
		}

		private void EnqueueInit(byte[] data)
		{
			bool flag = !this.DoFraming;
			if (!flag)
			{
				StreamBuffer streamBuffer = new StreamBuffer(data.Length + 32);
				BinaryWriter binaryWriter = new BinaryWriter(streamBuffer);
				byte[] array = new byte[]
				{
					251,
					0,
					0,
					0,
					0,
					0,
					1
				};
				int num = 1;
				Protocol.Serialize(data.Length + array.Length, array, ref num);
				binaryWriter.Write(array);
				binaryWriter.Write(data);
				byte[] array2 = streamBuffer.ToArray();
				bool trafficStatsEnabled = base.TrafficStatsEnabled;
				if (trafficStatsEnabled)
				{
					TrafficStats expr_79 = base.TrafficStatsOutgoing;
					int num2 = expr_79.TotalPacketCount;
					expr_79.TotalPacketCount = num2 + 1;
					TrafficStats expr_91 = base.TrafficStatsOutgoing;
					num2 = expr_91.TotalCommandsInPackets;
					expr_91.TotalCommandsInPackets = num2 + 1;
					base.TrafficStatsOutgoing.CountControlCommand(array2.Length);
				}
				this.EnqueueMessageAsPayload(true, array2, 0);
			}
		}

		internal override bool DispatchIncomingCommands()
		{
			while (true)
			{
				Queue<PeerBase.MyAction> actionQueue = this.ActionQueue;
				PeerBase.MyAction myAction;
				lock (actionQueue)
				{
					bool flag = this.ActionQueue.Count <= 0;
					if (flag)
					{
						break;
					}
					myAction = this.ActionQueue.Dequeue();
				}
				myAction();
			}
			Queue<byte[]> obj = this.incomingList;
			bool result;
			byte[] array;
			lock (obj)
			{
				bool flag2 = this.incomingList.Count <= 0;
				if (flag2)
				{
					result = false;
					return result;
				}
				array = this.incomingList.Dequeue();
			}
			this.ByteCountCurrentDispatch = array.Length + 3;
			result = this.DeserializeMessageAndCallback(array);
			return result;
		}

		internal override bool SendOutgoingCommands()
		{
			bool flag = this.peerConnectionState == PeerBase.ConnectionStateValue.Disconnected;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool flag2 = !this.rt.Connected;
				if (flag2)
				{
					result = false;
				}
				else
				{
					this.timeInt = SupportClass.GetTickCount() - this.timeBase;
					this.timeLastSendOutgoing = this.timeInt;
					bool flag3 = this.peerConnectionState == PeerBase.ConnectionStateValue.Connected && SupportClass.GetTickCount() - this.lastPingResult > base.timePingInterval;
					if (flag3)
					{
						this.SendPing();
					}
					List<byte[]> obj = this.outgoingStream;
					lock (obj)
					{
						foreach (byte[] current in this.outgoingStream)
						{
							this.SendData(current);
						}
						this.outgoingStream.Clear();
						this.outgoingCommandsInStream = 0;
					}
					result = false;
				}
			}
			return result;
		}

		internal override bool SendAcksOnly()
		{
			bool flag = this.rt == null || !this.rt.Connected;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				this.timeInt = SupportClass.GetTickCount() - this.timeBase;
				bool flag2 = this.peerConnectionState == PeerBase.ConnectionStateValue.Connected && SupportClass.GetTickCount() - this.lastPingResult > base.timePingInterval;
				if (flag2)
				{
					this.SendPing();
				}
				result = false;
			}
			return result;
		}

		internal override bool EnqueueOperation(Dictionary<byte, object> parameters, byte opCode, bool sendReliable, byte channelId, bool encrypt, PeerBase.EgMessageType messageType)
		{
			bool flag = this.peerConnectionState != PeerBase.ConnectionStateValue.Connected;
			bool result;
			if (flag)
			{
				bool flag2 = base.debugOut >= DebugLevel.ERROR;
				if (flag2)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, string.Concat(new object[]
					{
						"Cannot send op: ",
						opCode,
						"! Not connected. PeerState: ",
						this.peerConnectionState
					}));
				}
				base.Listener.OnStatusChanged(StatusCode.SendError);
				result = false;
			}
			else
			{
				bool flag3 = channelId >= base.ChannelCount;
				if (flag3)
				{
					bool flag4 = base.debugOut >= DebugLevel.ERROR;
					if (flag4)
					{
						base.Listener.DebugReturn(DebugLevel.ERROR, string.Concat(new object[]
						{
							"Cannot send op: Selected channel (",
							channelId,
							")>= channelCount (",
							base.ChannelCount,
							")."
						}));
					}
					base.Listener.OnStatusChanged(StatusCode.SendError);
					result = false;
				}
				else
				{
					byte[] opMessage = this.SerializeOperationToMessage(opCode, parameters, messageType, encrypt);
					result = this.EnqueueMessageAsPayload(sendReliable, opMessage, channelId);
				}
			}
			return result;
		}

		internal override byte[] SerializeOperationToMessage(byte opc, Dictionary<byte, object> parameters, PeerBase.EgMessageType messageType, bool encrypt)
		{
			StreamBuffer serializeMemStream = this.SerializeMemStream;
			byte[] array2;
			lock (serializeMemStream)
			{
				this.SerializeMemStream.SetLength(0L);
				bool flag = !encrypt;
				if (flag)
				{
					this.SerializeMemStream.Write(this.messageHeader, 0, this.messageHeader.Length);
				}
				this.protocol.SerializeOperationRequest(this.SerializeMemStream, opc, parameters, false);
				if (encrypt)
				{
					byte[] array = this.CryptoProvider.Encrypt(this.SerializeMemStream.GetBuffer(), 0, (int)this.SerializeMemStream.Length);
					this.SerializeMemStream.SetLength(0L);
					this.SerializeMemStream.Write(this.messageHeader, 0, this.messageHeader.Length);
					this.SerializeMemStream.Write(array, 0, array.Length);
				}
				array2 = this.SerializeMemStream.ToArray();
			}
			bool flag2 = messageType != PeerBase.EgMessageType.Operation;
			if (flag2)
			{
				array2[this.messageHeader.Length - 1] = (byte)messageType;
			}
			if (encrypt)
			{
				array2[this.messageHeader.Length - 1] = (array2[this.messageHeader.Length - 1] | 128);
			}
			bool doFraming = this.DoFraming;
			if (doFraming)
			{
				int num = 1;
				Protocol.Serialize(array2.Length, array2, ref num);
			}
			return array2;
		}

		internal bool EnqueueMessageAsPayload(bool sendReliable, byte[] opMessage, byte channelId)
		{
			bool flag = opMessage == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool doFraming = this.DoFraming;
				if (doFraming)
				{
					opMessage[5] = channelId;
					opMessage[6] = (sendReliable ? 1 : 0);
				}
				List<byte[]> obj = this.outgoingStream;
				lock (obj)
				{
					this.outgoingStream.Add(opMessage);
					this.outgoingCommandsInStream++;
				}
				int num = opMessage.Length;
				this.ByteCountLastOperation = num;
				bool trafficStatsEnabled = base.TrafficStatsEnabled;
				if (trafficStatsEnabled)
				{
					if (sendReliable)
					{
						base.TrafficStatsOutgoing.CountReliableOpCommand(num);
					}
					else
					{
						base.TrafficStatsOutgoing.CountUnreliableOpCommand(num);
					}
					base.TrafficStatsGameLevel.CountOperation(num);
				}
				result = true;
			}
			return result;
		}

		internal void SendPing()
		{
			this.lastPingResult = SupportClass.GetTickCount();
			bool flag = !this.DoFraming;
			if (flag)
			{
				int tickCount = SupportClass.GetTickCount();
				this.EnqueueOperation(new Dictionary<byte, object>
				{
					{
						1,
						tickCount
					}
				}, PhotonCodes.Ping, true, 0, false, PeerBase.EgMessageType.InternalOperationRequest);
			}
			else
			{
				int num = 1;
				Protocol.Serialize(SupportClass.GetTickCount(), this.pingRequest, ref num);
				bool trafficStatsEnabled = base.TrafficStatsEnabled;
				if (trafficStatsEnabled)
				{
					base.TrafficStatsOutgoing.CountControlCommand(this.pingRequest.Length);
				}
				this.SendData(this.pingRequest);
			}
		}

		internal void SendData(byte[] data)
		{
			try
			{
				this.bytesOut += (long)data.Length;
				bool trafficStatsEnabled = base.TrafficStatsEnabled;
				if (trafficStatsEnabled)
				{
					TrafficStats expr_3D = base.TrafficStatsOutgoing;
					int totalPacketCount = expr_3D.TotalPacketCount;
					expr_3D.TotalPacketCount = totalPacketCount + 1;
					base.TrafficStatsOutgoing.TotalCommandsInPackets += this.outgoingCommandsInStream;
				}
				bool isSimulationEnabled = base.NetworkSimulationSettings.IsSimulationEnabled;
				if (isSimulationEnabled)
				{
					base.SendNetworkSimulated(delegate
					{
						this.rt.Send(data, data.Length);
					});
				}
				else
				{
					this.rt.Send(data, data.Length);
				}
			}
			catch (Exception ex)
			{
				bool flag = base.debugOut >= DebugLevel.ERROR;
				if (flag)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, ex.ToString());
				}
				SupportClass.WriteStackTrace(ex);
			}
		}

		internal override void ReceiveIncomingCommands(byte[] inbuff, int dataLength)
		{
			bool flag = inbuff == null;
			if (flag)
			{
				bool flag2 = base.debugOut >= DebugLevel.ERROR;
				if (flag2)
				{
					base.EnqueueDebugReturn(DebugLevel.ERROR, "checkAndQueueIncomingCommands() inBuff: null");
				}
			}
			else
			{
				this.timestampOfLastReceive = SupportClass.GetTickCount();
				this.timeInt = SupportClass.GetTickCount() - this.timeBase;
				this.bytesIn += (long)(inbuff.Length + 7);
				bool trafficStatsEnabled = base.TrafficStatsEnabled;
				if (trafficStatsEnabled)
				{
					TrafficStats expr_6F = base.TrafficStatsIncoming;
					int num = expr_6F.TotalPacketCount;
					expr_6F.TotalPacketCount = num + 1;
					TrafficStats expr_85 = base.TrafficStatsIncoming;
					num = expr_85.TotalCommandsInPackets;
					expr_85.TotalCommandsInPackets = num + 1;
				}
				bool flag3 = inbuff[0] == 243 || inbuff[0] == 244;
				if (flag3)
				{
					Queue<byte[]> obj = this.incomingList;
					lock (obj)
					{
						this.incomingList.Enqueue(inbuff);
					}
				}
				else
				{
					bool flag4 = inbuff[0] == 240;
					if (flag4)
					{
						base.TrafficStatsIncoming.CountControlCommand(inbuff.Length);
						this.ReadPingResult(inbuff);
					}
					else
					{
						bool flag5 = base.debugOut >= DebugLevel.ERROR;
						if (flag5)
						{
							base.EnqueueDebugReturn(DebugLevel.ERROR, "receiveIncomingCommands() MagicNumber should be 0xF0, 0xF3 or 0xF4. Is: " + inbuff[0]);
						}
					}
				}
			}
		}

		private void ReadPingResult(byte[] inbuff)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 1;
			Protocol.Deserialize(out num, inbuff, ref num3);
			Protocol.Deserialize(out num2, inbuff, ref num3);
			this.lastRoundTripTime = SupportClass.GetTickCount() - num2;
			bool flag = !this.serverTimeOffsetIsAvailable;
			if (flag)
			{
				this.roundTripTime = this.lastRoundTripTime;
			}
			base.UpdateRoundTripTimeAndVariance(this.lastRoundTripTime);
			bool flag2 = !this.serverTimeOffsetIsAvailable;
			if (flag2)
			{
				this.serverTimeOffset = num + (this.lastRoundTripTime >> 1) - SupportClass.GetTickCount();
				this.serverTimeOffsetIsAvailable = true;
			}
		}

		protected internal void ReadPingResult(OperationResponse operationResponse)
		{
			int num = (int)operationResponse.Parameters[2];
			int num2 = (int)operationResponse.Parameters[1];
			this.lastRoundTripTime = SupportClass.GetTickCount() - num2;
			bool flag = !this.serverTimeOffsetIsAvailable;
			if (flag)
			{
				this.roundTripTime = this.lastRoundTripTime;
			}
			base.UpdateRoundTripTimeAndVariance(this.lastRoundTripTime);
			bool flag2 = !this.serverTimeOffsetIsAvailable;
			if (flag2)
			{
				this.serverTimeOffset = num + (this.lastRoundTripTime >> 1) - SupportClass.GetTickCount();
				this.serverTimeOffsetIsAvailable = true;
			}
		}
	}
}
