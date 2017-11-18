using ExitGames.Client.Photon.EncryptorManaged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ExitGames.Client.Photon
{
	internal class EnetPeer : PeerBase
	{
		private const int CRC_LENGTH = 4;

		private static readonly int HMAC_SIZE = 32;

		private static readonly int BLOCK_SIZE = 16;

		private static readonly int IV_SIZE = 16;

		private const int EncryptedDataGramHeaderSize = 7;

		private const int EncryptedHeaderSize = 5;

		private List<NCommand> sentReliableCommands = new List<NCommand>();

		private StreamBuffer outgoingAcknowledgementsPool;

		internal readonly int windowSize = 128;

		private byte udpCommandCount;

		private byte[] udpBuffer;

		private int udpBufferIndex;

		private int udpBufferLength;

		private byte[] bufferForEncryption;

		internal int challenge;

		internal int reliableCommandsRepeated;

		internal int reliableCommandsSent;

		internal int serverSentTime;

		internal static readonly byte[] udpHeader0xF3 = new byte[]
		{
			243,
			2
		};

		internal static readonly byte[] messageHeader = EnetPeer.udpHeader0xF3;

		protected bool datagramEncryptedConnection;

		private EnetChannel[] channelArray = new EnetChannel[0];

		private const byte ControlChannelNumber = 255;

		protected internal const short PeerIdForConnect = -1;

		protected internal const short PeerIdForConnectTrace = -2;

		private Queue<int> commandsToRemove = new Queue<int>();

		private Queue<NCommand> commandsToResend = new Queue<NCommand>();

		internal override int QueuedIncomingCommandsCount
		{
			get
			{
				int num = 0;
				EnetChannel[] obj = this.channelArray;
				lock (obj)
				{
					for (int i = 0; i < this.channelArray.Length; i++)
					{
						EnetChannel enetChannel = this.channelArray[i];
						num += enetChannel.incomingReliableCommandsList.Count;
						num += enetChannel.incomingUnreliableCommandsList.Count;
					}
				}
				return num;
			}
		}

		internal override int QueuedOutgoingCommandsCount
		{
			get
			{
				int num = 0;
				EnetChannel[] obj = this.channelArray;
				lock (obj)
				{
					for (int i = 0; i < this.channelArray.Length; i++)
					{
						EnetChannel enetChannel = this.channelArray[i];
						num += enetChannel.outgoingReliableCommandsList.Count;
						num += enetChannel.outgoingUnreliableCommandsList.Count;
					}
				}
				return num;
			}
		}

		private Encryptor encryptor
		{
			get
			{
				return this.ppeer.encryptor;
			}
		}

		private Decryptor decryptor
		{
			get
			{
				return this.ppeer.decryptor;
			}
		}

		internal EnetPeer()
		{
			PeerBase.peerCount += 1;
			base.InitOnce();
			this.TrafficPackageHeaderSize = 12;
		}

		internal override void InitPeerBase()
		{
			base.InitPeerBase();
			bool flag = this.ppeer.PayloadEncryptionSecret != null && this.usedProtocol == ConnectionProtocol.Udp;
			if (flag)
			{
				this.InitEncryption(this.ppeer.PayloadEncryptionSecret);
			}
			bool flag2 = this.encryptor != null && this.decryptor != null;
			if (flag2)
			{
				this.isEncryptionAvailable = true;
			}
			this.peerID = (this.ppeer.EnableServerTracing ? -2 : -1);
			this.challenge = SupportClass.ThreadSafeRandom.Next();
			bool flag3 = this.udpBuffer == null || this.udpBuffer.Length != base.mtu;
			if (flag3)
			{
				this.udpBuffer = new byte[base.mtu];
			}
			this.reliableCommandsSent = 0;
			this.reliableCommandsRepeated = 0;
			EnetChannel[] obj = this.channelArray;
			lock (obj)
			{
				EnetChannel[] array = this.channelArray;
				bool flag4 = array.Length != (int)(base.ChannelCount + 1);
				if (flag4)
				{
					array = new EnetChannel[(int)(base.ChannelCount + 1)];
				}
				for (byte b = 0; b < base.ChannelCount; b += 1)
				{
					array[(int)b] = new EnetChannel(b, this.commandBufferSize);
				}
				array[(int)base.ChannelCount] = new EnetChannel(255, this.commandBufferSize);
				this.channelArray = array;
			}
			List<NCommand> obj2 = this.sentReliableCommands;
			lock (obj2)
			{
				this.sentReliableCommands = new List<NCommand>(this.commandBufferSize);
			}
			this.outgoingAcknowledgementsPool = new StreamBuffer(0);
			base.CommandLogInit();
		}

		internal override bool Connect(string ipport, string appID, object custom = null)
		{
			bool flag = this.peerConnectionState > PeerBase.ConnectionStateValue.Disconnected;
			bool result;
			if (flag)
			{
				base.Listener.DebugReturn(DebugLevel.WARNING, "Connect() can't be called if peer is not Disconnected. Not connecting. peerConnectionState: " + this.peerConnectionState);
				result = false;
			}
			else
			{
				bool flag2 = base.debugOut >= DebugLevel.ALL;
				if (flag2)
				{
					base.Listener.DebugReturn(DebugLevel.ALL, "Connect()");
				}
				base.ServerAddress = ipport;
				this.InitPeerBase();
				bool flag3 = base.SocketImplementation != null;
				if (flag3)
				{
					this.rt = (IPhotonSocket)Activator.CreateInstance(base.SocketImplementation, new object[]
					{
						this
					});
				}
				else
				{
					this.rt = new SocketUdp(this);
				}
				bool flag4 = this.rt == null;
				if (flag4)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, "Connect() failed, because SocketImplementation or socket was null. Set PhotonPeer.SocketImplementation before Connect().");
					result = false;
				}
				else
				{
					bool flag5 = this.rt.Connect();
					if (flag5)
					{
						bool trafficStatsEnabled = base.TrafficStatsEnabled;
						if (trafficStatsEnabled)
						{
							base.TrafficStatsOutgoing.ControlCommandBytes += 44;
							base.TrafficStatsOutgoing.ControlCommandCount++;
						}
						this.peerConnectionState = PeerBase.ConnectionStateValue.Connecting;
						result = true;
					}
					else
					{
						result = false;
					}
				}
			}
			return result;
		}

		public override void OnConnect()
		{
			this.QueueOutgoingReliableCommand(new NCommand(this, 2, null, 255));
		}

		internal override void Disconnect()
		{
			bool flag = this.peerConnectionState == PeerBase.ConnectionStateValue.Disconnected || this.peerConnectionState == PeerBase.ConnectionStateValue.Disconnecting;
			if (!flag)
			{
				bool flag2 = this.sentReliableCommands != null;
				if (flag2)
				{
					List<NCommand> obj = this.sentReliableCommands;
					lock (obj)
					{
						this.sentReliableCommands.Clear();
					}
				}
				EnetChannel[] obj2 = this.channelArray;
				lock (obj2)
				{
					EnetChannel[] array = this.channelArray;
					for (int i = 0; i < array.Length; i++)
					{
						EnetChannel enetChannel = array[i];
						enetChannel.clearAll();
					}
				}
				bool isSimulationEnabled = base.NetworkSimulationSettings.IsSimulationEnabled;
				base.NetworkSimulationSettings.IsSimulationEnabled = false;
				NCommand nCommand = new NCommand(this, 4, null, 255);
				this.QueueOutgoingReliableCommand(nCommand);
				this.SendOutgoingCommands();
				bool trafficStatsEnabled = base.TrafficStatsEnabled;
				if (trafficStatsEnabled)
				{
					base.TrafficStatsOutgoing.CountControlCommand(nCommand.Size);
				}
				base.NetworkSimulationSettings.IsSimulationEnabled = isSimulationEnabled;
				this.rt.Disconnect();
				this.peerConnectionState = PeerBase.ConnectionStateValue.Disconnected;
				base.EnqueueStatusCallback(StatusCode.Disconnect);
				this.datagramEncryptedConnection = false;
			}
		}

		internal override void StopConnection()
		{
			bool flag = this.rt != null;
			if (flag)
			{
				this.rt.Disconnect();
			}
			this.peerConnectionState = PeerBase.ConnectionStateValue.Disconnected;
			bool flag2 = base.Listener != null;
			if (flag2)
			{
				base.Listener.OnStatusChanged(StatusCode.Disconnect);
			}
		}

		internal override void FetchServerTimestamp()
		{
			bool flag = this.peerConnectionState != PeerBase.ConnectionStateValue.Connected || !this.ApplicationIsInitialized;
			if (flag)
			{
				bool flag2 = base.debugOut >= DebugLevel.INFO;
				if (flag2)
				{
					base.EnqueueDebugReturn(DebugLevel.INFO, "FetchServerTimestamp() was skipped, as the client is not connected. Current ConnectionState: " + this.peerConnectionState);
				}
			}
			else
			{
				this.CreateAndEnqueueCommand(12, new byte[0], 255);
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
			NCommand nCommand = null;
			EnetChannel[] obj = this.channelArray;
			lock (obj)
			{
				for (int i = 0; i < this.channelArray.Length; i++)
				{
					EnetChannel enetChannel = this.channelArray[i];
					bool flag2 = enetChannel.incomingUnreliableCommandsList.Count > 0;
					if (flag2)
					{
						int num = 2147483647;
						foreach (int current in enetChannel.incomingUnreliableCommandsList.Keys)
						{
							NCommand nCommand2 = enetChannel.incomingUnreliableCommandsList[current];
							bool flag3 = current < enetChannel.incomingUnreliableSequenceNumber || nCommand2.reliableSequenceNumber < enetChannel.incomingReliableSequenceNumber;
							if (flag3)
							{
								this.commandsToRemove.Enqueue(current);
							}
							else
							{
								bool flag4 = base.limitOfUnreliableCommands > 0 && enetChannel.incomingUnreliableCommandsList.Count > base.limitOfUnreliableCommands;
								if (flag4)
								{
									this.commandsToRemove.Enqueue(current);
								}
								else
								{
									bool flag5 = current < num;
									if (flag5)
									{
										bool flag6 = nCommand2.reliableSequenceNumber > enetChannel.incomingReliableSequenceNumber;
										if (!flag6)
										{
											num = current;
										}
									}
								}
							}
						}
						while (this.commandsToRemove.Count > 0)
						{
							enetChannel.incomingUnreliableCommandsList.Remove(this.commandsToRemove.Dequeue());
						}
						bool flag7 = num < 2147483647;
						if (flag7)
						{
							nCommand = enetChannel.incomingUnreliableCommandsList[num];
						}
						bool flag8 = nCommand != null;
						if (flag8)
						{
							enetChannel.incomingUnreliableCommandsList.Remove(nCommand.unreliableSequenceNumber);
							enetChannel.incomingUnreliableSequenceNumber = nCommand.unreliableSequenceNumber;
							break;
						}
					}
					bool flag9 = nCommand == null && enetChannel.incomingReliableCommandsList.Count > 0;
					if (flag9)
					{
						enetChannel.incomingReliableCommandsList.TryGetValue(enetChannel.incomingReliableSequenceNumber + 1, out nCommand);
						bool flag10 = nCommand == null;
						if (!flag10)
						{
							bool flag11 = nCommand.commandType != 8;
							if (flag11)
							{
								enetChannel.incomingReliableSequenceNumber = nCommand.reliableSequenceNumber;
								enetChannel.incomingReliableCommandsList.Remove(nCommand.reliableSequenceNumber);
								break;
							}
							bool flag12 = nCommand.fragmentsRemaining > 0;
							if (flag12)
							{
								nCommand = null;
							}
							else
							{
								byte[] array = new byte[nCommand.totalLength];
								for (int j = nCommand.startSequenceNumber; j < nCommand.startSequenceNumber + nCommand.fragmentCount; j++)
								{
									bool flag13 = enetChannel.ContainsReliableSequenceNumber(j);
									if (!flag13)
									{
										throw new Exception("command.fragmentsRemaining was 0, but not all fragments are found to be combined!");
									}
									NCommand nCommand3 = enetChannel.FetchReliableSequenceNumber(j);
									Buffer.BlockCopy(nCommand3.Payload, 0, array, nCommand3.fragmentOffset, nCommand3.Payload.Length);
									enetChannel.incomingReliableCommandsList.Remove(nCommand3.reliableSequenceNumber);
								}
								bool flag14 = base.debugOut >= DebugLevel.ALL;
								if (flag14)
								{
									base.Listener.DebugReturn(DebugLevel.ALL, "assembled fragmented payload from " + nCommand.fragmentCount + " parts. Dispatching now.");
								}
								nCommand.Payload = array;
								nCommand.Size = 12 * nCommand.fragmentCount + nCommand.totalLength;
								enetChannel.incomingReliableSequenceNumber = nCommand.reliableSequenceNumber + nCommand.fragmentCount - 1;
							}
							break;
						}
					}
				}
			}
			bool flag15 = nCommand != null && nCommand.Payload != null;
			bool result;
			if (flag15)
			{
				this.ByteCountCurrentDispatch = nCommand.Size;
				this.CommandInCurrentDispatch = nCommand;
				bool flag16 = this.DeserializeMessageAndCallback(nCommand.Payload);
				if (flag16)
				{
					this.CommandInCurrentDispatch = null;
					result = true;
					return result;
				}
				this.CommandInCurrentDispatch = null;
			}
			result = false;
			return result;
		}

		private int GetFragmentLength()
		{
			int num = base.mtu;
			bool flag = this.datagramEncryptedConnection;
			int result;
			if (flag)
			{
				num = num - 7 - EnetPeer.HMAC_SIZE - EnetPeer.IV_SIZE;
				num = num / EnetPeer.BLOCK_SIZE * EnetPeer.BLOCK_SIZE;
				num = num - 5 - 36;
				result = num;
			}
			else
			{
				result = num - 12 - 36;
			}
			return result;
		}

		private int CalculateBufferLen()
		{
			int num = base.mtu;
			bool flag = this.datagramEncryptedConnection;
			int result;
			if (flag)
			{
				num = num - 7 - EnetPeer.HMAC_SIZE - EnetPeer.IV_SIZE;
				num = num / EnetPeer.BLOCK_SIZE * EnetPeer.BLOCK_SIZE;
				num--;
				result = num;
			}
			else
			{
				result = num;
			}
			return result;
		}

		private int CalculateInitialOffset()
		{
			bool flag = this.datagramEncryptedConnection;
			int result;
			if (flag)
			{
				result = 5;
			}
			else
			{
				int num = 12;
				bool crcEnabled = base.crcEnabled;
				if (crcEnabled)
				{
					num += 4;
				}
				result = num;
			}
			return result;
		}

		internal override bool SendAcksOnly()
		{
			bool flag = this.peerConnectionState == PeerBase.ConnectionStateValue.Disconnected;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				bool flag2 = this.rt == null || !this.rt.Connected;
				if (flag2)
				{
					result = false;
				}
				else
				{
					byte[] obj = this.udpBuffer;
					lock (obj)
					{
						int num = 0;
						this.udpBufferIndex = this.CalculateInitialOffset();
						this.udpBufferLength = this.CalculateBufferLen();
						this.udpCommandCount = 0;
						this.timeInt = SupportClass.GetTickCount() - this.timeBase;
						StreamBuffer obj2 = this.outgoingAcknowledgementsPool;
						lock (obj2)
						{
							num = this.SerializeAckToBuffer();
							this.timeLastSendAck = this.timeInt;
						}
						bool flag3 = this.timeInt > this.timeoutInt && this.sentReliableCommands.Count > 0;
						if (flag3)
						{
							List<NCommand> obj3 = this.sentReliableCommands;
							lock (obj3)
							{
								foreach (NCommand current in this.sentReliableCommands)
								{
									bool flag4 = current != null && current.roundTripTimeout != 0 && this.timeInt - current.commandSentTime > current.roundTripTimeout;
									if (flag4)
									{
										current.commandSentCount = 1;
										current.roundTripTimeout = 0;
										current.timeoutTime = 2147483647;
										current.commandSentTime = this.timeInt;
									}
								}
							}
						}
						bool flag5 = this.udpCommandCount <= 0;
						if (flag5)
						{
							result = false;
						}
						else
						{
							bool trafficStatsEnabled = base.TrafficStatsEnabled;
							if (trafficStatsEnabled)
							{
								TrafficStats expr_1AD = base.TrafficStatsOutgoing;
								int totalPacketCount = expr_1AD.TotalPacketCount;
								expr_1AD.TotalPacketCount = totalPacketCount + 1;
								base.TrafficStatsOutgoing.TotalCommandsInPackets += (int)this.udpCommandCount;
							}
							this.SendData(this.udpBuffer, this.udpBufferIndex);
							result = (num > 0);
						}
					}
				}
			}
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
					byte[] obj = this.udpBuffer;
					lock (obj)
					{
						int num = 0;
						this.udpBufferIndex = this.CalculateInitialOffset();
						this.udpBufferLength = this.CalculateBufferLen();
						this.udpCommandCount = 0;
						this.timeInt = SupportClass.GetTickCount() - this.timeBase;
						this.timeLastSendOutgoing = this.timeInt;
						StreamBuffer obj2 = this.outgoingAcknowledgementsPool;
						lock (obj2)
						{
							bool flag3 = this.outgoingAcknowledgementsPool.Length > 0L;
							if (flag3)
							{
								num = this.SerializeAckToBuffer();
								this.timeLastSendAck = this.timeInt;
							}
						}
						bool flag4 = !base.IsSendingOnlyAcks && this.timeInt > this.timeoutInt && this.sentReliableCommands.Count > 0;
						if (flag4)
						{
							List<NCommand> obj3 = this.sentReliableCommands;
							lock (obj3)
							{
								this.commandsToResend.Clear();
								foreach (NCommand current in this.sentReliableCommands)
								{
									bool flag5 = current != null && this.timeInt - current.commandSentTime > current.roundTripTimeout;
									if (flag5)
									{
										bool flag6 = (int)current.commandSentCount > base.sentCountAllowance || this.timeInt > current.timeoutTime;
										if (flag6)
										{
											bool flag7 = base.debugOut >= DebugLevel.WARNING;
											if (flag7)
											{
												base.Listener.DebugReturn(DebugLevel.WARNING, string.Concat(new object[]
												{
													"Timeout-disconnect! Command: ",
													current,
													" now: ",
													this.timeInt,
													" challenge: ",
													Convert.ToString(this.challenge, 16)
												}));
											}
											bool flag8 = this.CommandLog != null;
											if (flag8)
											{
												this.CommandLog.Enqueue(new CmdLogSentReliable(current, this.timeInt, this.roundTripTime, this.roundTripTimeVariance, true));
												base.CommandLogResize();
											}
											this.peerConnectionState = PeerBase.ConnectionStateValue.Zombie;
											base.Listener.OnStatusChanged(StatusCode.TimeoutDisconnect);
											this.Disconnect();
											result = false;
											return result;
										}
										this.commandsToResend.Enqueue(current);
									}
								}
								while (this.commandsToResend.Count > 0)
								{
									NCommand nCommand = this.commandsToResend.Dequeue();
									this.QueueOutgoingReliableCommand(nCommand);
									this.sentReliableCommands.Remove(nCommand);
									this.reliableCommandsRepeated++;
									bool flag9 = base.debugOut >= DebugLevel.INFO;
									if (flag9)
									{
										base.Listener.DebugReturn(DebugLevel.INFO, string.Format("Resending: {0}. times out after: {1} sent: {3} now: {2} rtt/var: {4}/{5} last recv: {6}", new object[]
										{
											nCommand,
											nCommand.roundTripTimeout,
											this.timeInt,
											nCommand.commandSentTime,
											this.roundTripTime,
											this.roundTripTimeVariance,
											SupportClass.GetTickCount() - this.timestampOfLastReceive
										}));
									}
								}
							}
						}
						bool flag10 = !base.IsSendingOnlyAcks && this.peerConnectionState == PeerBase.ConnectionStateValue.Connected && base.timePingInterval > 0 && this.sentReliableCommands.Count == 0 && this.timeInt - this.timeLastAckReceive > base.timePingInterval && !this.AreReliableCommandsInTransit() && this.udpBufferIndex + 12 < this.udpBufferLength;
						if (flag10)
						{
							NCommand nCommand2 = new NCommand(this, 5, null, 255);
							this.QueueOutgoingReliableCommand(nCommand2);
							bool trafficStatsEnabled = base.TrafficStatsEnabled;
							if (trafficStatsEnabled)
							{
								base.TrafficStatsOutgoing.CountControlCommand(nCommand2.Size);
							}
						}
						bool flag11 = !base.IsSendingOnlyAcks;
						if (flag11)
						{
							EnetChannel[] obj4 = this.channelArray;
							lock (obj4)
							{
								for (int i = 0; i < this.channelArray.Length; i++)
								{
									EnetChannel enetChannel = this.channelArray[i];
									num += this.SerializeToBuffer(enetChannel.outgoingReliableCommandsList);
									num += this.SerializeToBuffer(enetChannel.outgoingUnreliableCommandsList);
								}
							}
						}
						bool flag12 = this.udpCommandCount <= 0;
						if (flag12)
						{
							result = false;
						}
						else
						{
							bool trafficStatsEnabled2 = base.TrafficStatsEnabled;
							if (trafficStatsEnabled2)
							{
								TrafficStats expr_4AE = base.TrafficStatsOutgoing;
								int totalPacketCount = expr_4AE.TotalPacketCount;
								expr_4AE.TotalPacketCount = totalPacketCount + 1;
								base.TrafficStatsOutgoing.TotalCommandsInPackets += (int)this.udpCommandCount;
							}
							this.SendData(this.udpBuffer, this.udpBufferIndex);
							result = (num > 0);
						}
					}
				}
			}
			return result;
		}

		private bool AreReliableCommandsInTransit()
		{
			EnetChannel[] obj = this.channelArray;
			bool result;
			lock (obj)
			{
				for (int i = 0; i < this.channelArray.Length; i++)
				{
					EnetChannel enetChannel = this.channelArray[i];
					bool flag = enetChannel.outgoingReliableCommandsList.Count > 0;
					if (flag)
					{
						result = true;
						return result;
					}
				}
			}
			result = false;
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
						" Not connected. PeerState: ",
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
					byte[] payload = this.SerializeOperationToMessage(opCode, parameters, messageType, encrypt);
					result = this.CreateAndEnqueueCommand(sendReliable ? 6 : 7, payload, channelId);
				}
			}
			return result;
		}

		private EnetChannel GetChannel(byte channelNumber)
		{
			return (channelNumber == 255) ? this.channelArray[this.channelArray.Length - 1] : this.channelArray[(int)channelNumber];
		}

		internal bool CreateAndEnqueueCommand(byte commandType, byte[] payload, byte channelNumber)
		{
			bool flag = payload == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				EnetChannel channel = this.GetChannel(channelNumber);
				this.ByteCountLastOperation = 0;
				int num = this.GetFragmentLength();
				bool flag2 = payload.Length > num;
				if (flag2)
				{
					int fragmentCount = (payload.Length + num - 1) / num;
					int startSequenceNumber = channel.outgoingReliableSequenceNumber + 1;
					int num2 = 0;
					for (int i = 0; i < payload.Length; i += num)
					{
						bool flag3 = payload.Length - i < num;
						if (flag3)
						{
							num = payload.Length - i;
						}
						byte[] array = new byte[num];
						Buffer.BlockCopy(payload, i, array, 0, num);
						NCommand nCommand = new NCommand(this, 8, array, channel.ChannelNumber);
						nCommand.fragmentNumber = num2;
						nCommand.startSequenceNumber = startSequenceNumber;
						nCommand.fragmentCount = fragmentCount;
						nCommand.totalLength = payload.Length;
						nCommand.fragmentOffset = i;
						this.QueueOutgoingReliableCommand(nCommand);
						this.ByteCountLastOperation += nCommand.Size;
						bool trafficStatsEnabled = base.TrafficStatsEnabled;
						if (trafficStatsEnabled)
						{
							base.TrafficStatsOutgoing.CountFragmentOpCommand(nCommand.Size);
							base.TrafficStatsGameLevel.CountOperation(nCommand.Size);
						}
						num2++;
					}
				}
				else
				{
					NCommand nCommand2 = new NCommand(this, commandType, payload, channel.ChannelNumber);
					bool flag4 = nCommand2.commandFlags == 1;
					if (flag4)
					{
						this.QueueOutgoingReliableCommand(nCommand2);
						this.ByteCountLastOperation = nCommand2.Size;
						bool trafficStatsEnabled2 = base.TrafficStatsEnabled;
						if (trafficStatsEnabled2)
						{
							base.TrafficStatsOutgoing.CountReliableOpCommand(nCommand2.Size);
							base.TrafficStatsGameLevel.CountOperation(nCommand2.Size);
						}
					}
					else
					{
						this.QueueOutgoingUnreliableCommand(nCommand2);
						this.ByteCountLastOperation = nCommand2.Size;
						bool trafficStatsEnabled3 = base.TrafficStatsEnabled;
						if (trafficStatsEnabled3)
						{
							base.TrafficStatsOutgoing.CountUnreliableOpCommand(nCommand2.Size);
							base.TrafficStatsGameLevel.CountOperation(nCommand2.Size);
						}
					}
				}
				result = true;
			}
			return result;
		}

		internal override byte[] SerializeOperationToMessage(byte opCode, Dictionary<byte, object> parameters, PeerBase.EgMessageType messageType, bool encrypt)
		{
			encrypt = (encrypt && !this.datagramEncryptedConnection);
			StreamBuffer serializeMemStream = this.SerializeMemStream;
			byte[] array2;
			lock (serializeMemStream)
			{
				this.SerializeMemStream.SetLength(0L);
				bool flag = !encrypt;
				if (flag)
				{
					this.SerializeMemStream.Write(EnetPeer.messageHeader, 0, EnetPeer.messageHeader.Length);
				}
				this.protocol.SerializeOperationRequest(this.SerializeMemStream, opCode, parameters, false);
				bool flag2 = encrypt;
				if (flag2)
				{
					byte[] array = this.CryptoProvider.Encrypt(this.SerializeMemStream.GetBuffer(), 0, (int)this.SerializeMemStream.Length);
					this.SerializeMemStream.SetLength(0L);
					this.SerializeMemStream.Write(EnetPeer.messageHeader, 0, EnetPeer.messageHeader.Length);
					this.SerializeMemStream.Write(array, 0, array.Length);
				}
				array2 = this.SerializeMemStream.ToArray();
			}
			bool flag3 = messageType != PeerBase.EgMessageType.Operation;
			if (flag3)
			{
				array2[EnetPeer.messageHeader.Length - 1] = (byte)messageType;
			}
			bool flag4 = encrypt;
			if (flag4)
			{
				array2[EnetPeer.messageHeader.Length - 1] = (array2[EnetPeer.messageHeader.Length - 1] | 128);
			}
			return array2;
		}

		internal int SerializeAckToBuffer()
		{
			this.outgoingAcknowledgementsPool.Seek(0L, SeekOrigin.Begin);
			while (this.outgoingAcknowledgementsPool.Position + 20L <= this.outgoingAcknowledgementsPool.Length)
			{
				bool flag = this.udpBufferIndex + 20 > this.udpBufferLength;
				if (flag)
				{
					bool flag2 = base.debugOut >= DebugLevel.INFO;
					if (flag2)
					{
						base.Listener.DebugReturn(DebugLevel.INFO, string.Concat(new object[]
						{
							"UDP package is full. Commands in Package: ",
							this.udpCommandCount,
							". bytes left in queue: ",
							this.outgoingAcknowledgementsPool.Position
						}));
					}
					break;
				}
				int srcOffset;
				byte[] bufferAndAdvance = this.outgoingAcknowledgementsPool.GetBufferAndAdvance(20, out srcOffset);
				Buffer.BlockCopy(bufferAndAdvance, srcOffset, this.udpBuffer, this.udpBufferIndex, 20);
				this.udpBufferIndex += 20;
				this.udpCommandCount += 1;
			}
			this.outgoingAcknowledgementsPool.Compact();
			this.outgoingAcknowledgementsPool.Position = this.outgoingAcknowledgementsPool.Length;
			return (int)this.outgoingAcknowledgementsPool.Length / 20;
		}

		internal int SerializeToBuffer(Queue<NCommand> commandList)
		{
			while (commandList.Count > 0)
			{
				NCommand nCommand = commandList.Peek();
				bool flag = nCommand == null;
				if (flag)
				{
					commandList.Dequeue();
				}
				else
				{
					bool flag2 = this.udpBufferIndex + nCommand.Size > this.udpBufferLength;
					if (flag2)
					{
						bool flag3 = base.debugOut >= DebugLevel.INFO;
						if (flag3)
						{
							base.Listener.DebugReturn(DebugLevel.INFO, string.Concat(new object[]
							{
								"UDP package is full. Commands in Package: ",
								this.udpCommandCount,
								". Commands left in queue: ",
								commandList.Count
							}));
						}
						break;
					}
					nCommand.SerializeHeader(this.udpBuffer, ref this.udpBufferIndex);
					bool flag4 = nCommand.SizeOfPayload > 0;
					if (flag4)
					{
						Buffer.BlockCopy(nCommand.Serialize(), 0, this.udpBuffer, this.udpBufferIndex, nCommand.SizeOfPayload);
						this.udpBufferIndex += nCommand.SizeOfPayload;
					}
					this.udpCommandCount += 1;
					bool flag5 = (nCommand.commandFlags & 1) > 0;
					if (flag5)
					{
						this.QueueSentCommand(nCommand);
						bool flag6 = this.CommandLog != null;
						if (flag6)
						{
							this.CommandLog.Enqueue(new CmdLogSentReliable(nCommand, this.timeInt, this.roundTripTime, this.roundTripTimeVariance, false));
							base.CommandLogResize();
						}
					}
					commandList.Dequeue();
				}
			}
			return commandList.Count;
		}

		internal void SendData(byte[] data, int length)
		{
			try
			{
				bool flag = this.datagramEncryptedConnection;
				if (flag)
				{
					this.SendDataEncrypted(data, length);
				}
				else
				{
					int num = 0;
					Protocol.Serialize(this.peerID, data, ref num);
					data[2] = (base.crcEnabled ? 204 : 0);
					data[3] = this.udpCommandCount;
					num = 4;
					Protocol.Serialize(this.timeInt, data, ref num);
					Protocol.Serialize(this.challenge, data, ref num);
					bool crcEnabled = base.crcEnabled;
					if (crcEnabled)
					{
						Protocol.Serialize(0, data, ref num);
						uint value = SupportClass.CalculateCrc(data, length);
						num -= 4;
						Protocol.Serialize((int)value, data, ref num);
					}
					this.bytesOut += (long)length;
					this.SendToSocket(data, length);
				}
			}
			catch (Exception ex)
			{
				bool flag2 = base.debugOut >= DebugLevel.ERROR;
				if (flag2)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, ex.ToString());
				}
				SupportClass.WriteStackTrace(ex);
			}
		}

		private void SendToSocket(byte[] data, int length)
		{
			bool isSimulationEnabled = base.NetworkSimulationSettings.IsSimulationEnabled;
			if (isSimulationEnabled)
			{
				byte[] dataCopy = new byte[length];
				Buffer.BlockCopy(data, 0, dataCopy, 0, length);
				base.SendNetworkSimulated(delegate
				{
					this.rt.Send(dataCopy, length);
				});
			}
			else
			{
				this.rt.Send(data, length);
			}
		}

		private void SendDataEncrypted(byte[] data, int length)
		{
			bool flag = this.bufferForEncryption == null || this.bufferForEncryption.Length != base.mtu;
			if (flag)
			{
				this.bufferForEncryption = new byte[base.mtu];
			}
			byte[] array = this.bufferForEncryption;
			int num = 0;
			Protocol.Serialize(this.peerID, array, ref num);
			array[2] = 1;
			num++;
			Protocol.Serialize(this.challenge, array, ref num);
			data[0] = this.udpCommandCount;
			int num2 = 1;
			Protocol.Serialize(this.timeInt, data, ref num2);
			this.encryptor.Encrypt(data, length, array, ref num);
			Buffer.BlockCopy(this.encryptor.FinishHMAC(array, 0, num), 0, array, num, EnetPeer.HMAC_SIZE);
			this.SendToSocket(array, num + EnetPeer.HMAC_SIZE);
		}

		internal void QueueSentCommand(NCommand command)
		{
			command.commandSentTime = this.timeInt;
			command.commandSentCount += 1;
			bool flag = command.roundTripTimeout == 0;
			if (flag)
			{
				command.roundTripTimeout = this.roundTripTime + 4 * this.roundTripTimeVariance;
				command.timeoutTime = this.timeInt + base.DisconnectTimeout;
			}
			else
			{
				bool flag2 = command.commandSentCount <= base.QuickResendAttempts + 1;
				if (!flag2)
				{
					command.roundTripTimeout *= 2;
				}
			}
			List<NCommand> obj = this.sentReliableCommands;
			lock (obj)
			{
				bool flag3 = this.sentReliableCommands.Count == 0;
				if (flag3)
				{
					int num = command.commandSentTime + command.roundTripTimeout;
					bool flag4 = num < this.timeoutInt;
					if (flag4)
					{
						this.timeoutInt = num;
					}
				}
				this.reliableCommandsSent++;
				this.sentReliableCommands.Add(command);
			}
		}

		internal void QueueOutgoingReliableCommand(NCommand command)
		{
			EnetChannel channel = this.GetChannel(command.commandChannelID);
			EnetChannel obj = channel;
			lock (obj)
			{
				bool flag = command.reliableSequenceNumber == 0;
				if (flag)
				{
					EnetChannel expr_28 = channel;
					int num = expr_28.outgoingReliableSequenceNumber + 1;
					expr_28.outgoingReliableSequenceNumber = num;
					command.reliableSequenceNumber = num;
				}
				channel.outgoingReliableCommandsList.Enqueue(command);
			}
		}

		internal void QueueOutgoingUnreliableCommand(NCommand command)
		{
			EnetChannel channel = this.GetChannel(command.commandChannelID);
			EnetChannel obj = channel;
			lock (obj)
			{
				command.reliableSequenceNumber = channel.outgoingReliableSequenceNumber;
				EnetChannel expr_26 = channel;
				int num = expr_26.outgoingUnreliableSequenceNumber + 1;
				expr_26.outgoingUnreliableSequenceNumber = num;
				command.unreliableSequenceNumber = num;
				channel.outgoingUnreliableCommandsList.Enqueue(command);
			}
		}

		internal void QueueOutgoingAcknowledgement(NCommand readCommand, int sendTime)
		{
			int offset;
			byte[] bufferAndAdvance = this.outgoingAcknowledgementsPool.GetBufferAndAdvance(20, out offset);
			NCommand.CreateAck(bufferAndAdvance, offset, readCommand, sendTime);
		}

		internal override void ReceiveIncomingCommands(byte[] inBuff, int dataLength)
		{
			this.timestampOfLastReceive = SupportClass.GetTickCount();
			try
			{
				int num = 0;
				short num2;
				Protocol.Deserialize(out num2, inBuff, ref num);
				byte b = inBuff[num++];
				bool flag = b == 1;
				int num3;
				byte b2;
				if (flag)
				{
					bool flag2 = this.decryptor == null;
					if (flag2)
					{
						base.EnqueueDebugReturn(DebugLevel.ERROR, "Got encrypted packet, but encryption is not set up. Packet ignored");
						return;
					}
					this.datagramEncryptedConnection = true;
					bool flag3 = !this.decryptor.CheckHMAC(inBuff, dataLength);
					if (flag3)
					{
						this.packetLossByCrc++;
						bool flag4 = this.peerConnectionState != PeerBase.ConnectionStateValue.Disconnected && base.debugOut >= DebugLevel.INFO;
						if (flag4)
						{
							base.EnqueueDebugReturn(DebugLevel.INFO, "Ignored package due to wrong HMAC.");
						}
						return;
					}
					Protocol.Deserialize(out num3, inBuff, ref num);
					inBuff = this.decryptor.DecryptBufferWithIV(inBuff, num, dataLength - num - EnetPeer.HMAC_SIZE, out dataLength);
					dataLength = inBuff.Length;
					num = 0;
					b2 = inBuff[num++];
					Protocol.Deserialize(out this.serverSentTime, inBuff, ref num);
					this.bytesIn += (long)(12 + EnetPeer.IV_SIZE + EnetPeer.HMAC_SIZE + dataLength + (EnetPeer.BLOCK_SIZE - dataLength % EnetPeer.BLOCK_SIZE));
				}
				else
				{
					bool flag5 = this.datagramEncryptedConnection;
					if (flag5)
					{
						base.EnqueueDebugReturn(DebugLevel.WARNING, "Got not encrypted packet, but expected only encrypted. Packet ignored");
						return;
					}
					b2 = inBuff[num++];
					Protocol.Deserialize(out this.serverSentTime, inBuff, ref num);
					Protocol.Deserialize(out num3, inBuff, ref num);
					bool flag6 = b == 204;
					if (flag6)
					{
						int num4;
						Protocol.Deserialize(out num4, inBuff, ref num);
						this.bytesIn += 4L;
						num -= 4;
						Protocol.Serialize(0, inBuff, ref num);
						uint num5 = SupportClass.CalculateCrc(inBuff, dataLength);
						bool flag7 = num4 != (int)num5;
						if (flag7)
						{
							this.packetLossByCrc++;
							bool flag8 = this.peerConnectionState != PeerBase.ConnectionStateValue.Disconnected && base.debugOut >= DebugLevel.INFO;
							if (flag8)
							{
								base.EnqueueDebugReturn(DebugLevel.INFO, string.Format("Ignored package due to wrong CRC. Incoming:  {0:X} Local: {1:X}", (uint)num4, num5));
							}
							return;
						}
					}
					this.bytesIn += 12L;
				}
				bool trafficStatsEnabled = base.TrafficStatsEnabled;
				if (trafficStatsEnabled)
				{
					TrafficStats expr_233 = base.TrafficStatsIncoming;
					int totalPacketCount = expr_233.TotalPacketCount;
					expr_233.TotalPacketCount = totalPacketCount + 1;
					base.TrafficStatsIncoming.TotalCommandsInPackets += (int)b2;
				}
				bool flag9 = (int)b2 > this.commandBufferSize || b2 <= 0;
				if (flag9)
				{
					base.EnqueueDebugReturn(DebugLevel.ERROR, string.Concat(new object[]
					{
						"too many/few incoming commands in package: ",
						b2,
						" > ",
						this.commandBufferSize
					}));
				}
				bool flag10 = num3 != this.challenge;
				if (flag10)
				{
					this.packetLossByChallenge++;
					bool flag11 = this.peerConnectionState != PeerBase.ConnectionStateValue.Disconnected && base.debugOut >= DebugLevel.ALL;
					if (flag11)
					{
						base.EnqueueDebugReturn(DebugLevel.ALL, string.Concat(new object[]
						{
							"Info: Ignoring received package due to wrong challenge. Challenge in-package!=local:",
							num3,
							"!=",
							this.challenge,
							" Commands in it: ",
							b2
						}));
					}
				}
				else
				{
					this.timeInt = SupportClass.GetTickCount() - this.timeBase;
					for (int i = 0; i < (int)b2; i++)
					{
						NCommand readCommand = new NCommand(this, inBuff, ref num);
						bool flag12 = readCommand.commandType != 1;
						if (flag12)
						{
							base.EnqueueActionForDispatch(delegate
							{
								this.ExecuteCommand(readCommand);
							});
						}
						else
						{
							this.ExecuteCommand(readCommand);
						}
						bool flag13 = (readCommand.commandFlags & 1) > 0;
						if (flag13)
						{
							bool flag14 = this.InReliableLog != null;
							if (flag14)
							{
								this.InReliableLog.Enqueue(new CmdLogReceivedReliable(readCommand, this.timeInt, this.roundTripTime, this.roundTripTimeVariance, this.timeInt - this.timeLastSendOutgoing, this.timeInt - this.timeLastSendAck));
								base.CommandLogResize();
							}
							this.QueueOutgoingAcknowledgement(readCommand, this.serverSentTime);
							bool trafficStatsEnabled2 = base.TrafficStatsEnabled;
							if (trafficStatsEnabled2)
							{
								base.TrafficStatsIncoming.TimestampOfLastReliableCommand = SupportClass.GetTickCount();
								base.TrafficStatsOutgoing.CountControlCommand(20);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				bool flag15 = base.debugOut >= DebugLevel.ERROR;
				if (flag15)
				{
					base.EnqueueDebugReturn(DebugLevel.ERROR, string.Format("Exception while reading commands from incoming data: {0}", ex));
				}
				SupportClass.WriteStackTrace(ex);
			}
		}

		internal bool ExecuteCommand(NCommand command)
		{
			bool flag = true;
			switch (command.commandType)
			{
			case 1:
			{
				bool trafficStatsEnabled = base.TrafficStatsEnabled;
				if (trafficStatsEnabled)
				{
					base.TrafficStatsIncoming.TimestampOfLastAck = SupportClass.GetTickCount();
					base.TrafficStatsIncoming.CountControlCommand(command.Size);
				}
				this.timeLastAckReceive = this.timeInt;
				this.lastRoundTripTime = this.timeInt - command.ackReceivedSentTime;
				NCommand nCommand = this.RemoveSentReliableCommand(command.ackReceivedReliableSequenceNumber, (int)command.commandChannelID);
				bool flag2 = this.CommandLog != null;
				if (flag2)
				{
					this.CommandLog.Enqueue(new CmdLogReceivedAck(command, this.timeInt, this.roundTripTime, this.roundTripTimeVariance));
					base.CommandLogResize();
				}
				bool flag3 = nCommand != null;
				if (flag3)
				{
					bool flag4 = nCommand.commandType == 12;
					if (flag4)
					{
						bool flag5 = this.lastRoundTripTime <= this.roundTripTime;
						if (flag5)
						{
							this.serverTimeOffset = this.serverSentTime + (this.lastRoundTripTime >> 1) - SupportClass.GetTickCount();
							this.serverTimeOffsetIsAvailable = true;
						}
						else
						{
							this.FetchServerTimestamp();
						}
					}
					else
					{
						base.UpdateRoundTripTimeAndVariance(this.lastRoundTripTime);
						bool flag6 = nCommand.commandType == 4 && this.peerConnectionState == PeerBase.ConnectionStateValue.Disconnecting;
						if (flag6)
						{
							bool flag7 = base.debugOut >= DebugLevel.INFO;
							if (flag7)
							{
								base.EnqueueDebugReturn(DebugLevel.INFO, "Received disconnect ACK by server");
							}
							base.EnqueueActionForDispatch(delegate
							{
								this.rt.Disconnect();
							});
						}
						else
						{
							bool flag8 = nCommand.commandType == 2;
							if (flag8)
							{
								this.roundTripTime = this.lastRoundTripTime;
							}
						}
					}
				}
				break;
			}
			case 2:
			case 5:
			{
				bool trafficStatsEnabled2 = base.TrafficStatsEnabled;
				if (trafficStatsEnabled2)
				{
					base.TrafficStatsIncoming.CountControlCommand(command.Size);
				}
				break;
			}
			case 3:
			{
				bool trafficStatsEnabled3 = base.TrafficStatsEnabled;
				if (trafficStatsEnabled3)
				{
					base.TrafficStatsIncoming.CountControlCommand(command.Size);
				}
				bool flag9 = this.peerConnectionState == PeerBase.ConnectionStateValue.Connecting;
				if (flag9)
				{
					byte[] payload = base.PrepareConnectData(base.ServerAddress, this.AppId, this.CustomInitData);
					this.CreateAndEnqueueCommand(6, payload, 0);
					this.peerConnectionState = PeerBase.ConnectionStateValue.Connected;
				}
				break;
			}
			case 4:
			{
				bool trafficStatsEnabled4 = base.TrafficStatsEnabled;
				if (trafficStatsEnabled4)
				{
					base.TrafficStatsIncoming.CountControlCommand(command.Size);
				}
				StatusCode statusCode = StatusCode.DisconnectByServer;
				bool flag10 = command.reservedByte == 1;
				if (flag10)
				{
					statusCode = StatusCode.DisconnectByServerLogic;
				}
				else
				{
					bool flag11 = command.reservedByte == 3;
					if (flag11)
					{
						statusCode = StatusCode.DisconnectByServerUserLimit;
					}
				}
				bool flag12 = base.debugOut >= DebugLevel.INFO;
				if (flag12)
				{
					base.Listener.DebugReturn(DebugLevel.INFO, string.Concat(new object[]
					{
						"Server ",
						base.ServerAddress,
						" sent disconnect. PeerId: ",
						(ushort)this.peerID,
						" RTT/Variance:",
						this.roundTripTime,
						"/",
						this.roundTripTimeVariance,
						" reason byte: ",
						command.reservedByte
					}));
				}
				PeerBase.ConnectionStateValue peerConnectionState = this.peerConnectionState;
				this.peerConnectionState = PeerBase.ConnectionStateValue.Disconnecting;
				base.Listener.OnStatusChanged(statusCode);
				this.peerConnectionState = peerConnectionState;
				this.Disconnect();
				break;
			}
			case 6:
			{
				bool trafficStatsEnabled5 = base.TrafficStatsEnabled;
				if (trafficStatsEnabled5)
				{
					base.TrafficStatsIncoming.CountReliableOpCommand(command.Size);
				}
				bool flag13 = this.peerConnectionState == PeerBase.ConnectionStateValue.Connected;
				if (flag13)
				{
					flag = this.QueueIncomingCommand(command);
				}
				break;
			}
			case 7:
			{
				bool trafficStatsEnabled6 = base.TrafficStatsEnabled;
				if (trafficStatsEnabled6)
				{
					base.TrafficStatsIncoming.CountUnreliableOpCommand(command.Size);
				}
				bool flag14 = this.peerConnectionState == PeerBase.ConnectionStateValue.Connected;
				if (flag14)
				{
					flag = this.QueueIncomingCommand(command);
				}
				break;
			}
			case 8:
			{
				bool trafficStatsEnabled7 = base.TrafficStatsEnabled;
				if (trafficStatsEnabled7)
				{
					base.TrafficStatsIncoming.CountFragmentOpCommand(command.Size);
				}
				bool flag15 = this.peerConnectionState == PeerBase.ConnectionStateValue.Connected;
				if (flag15)
				{
					bool flag16 = command.fragmentNumber > command.fragmentCount || command.fragmentOffset >= command.totalLength || command.fragmentOffset + command.Payload.Length > command.totalLength;
					if (flag16)
					{
						bool flag17 = base.debugOut >= DebugLevel.ERROR;
						if (flag17)
						{
							base.Listener.DebugReturn(DebugLevel.ERROR, "Received fragment has bad size: " + command);
						}
					}
					else
					{
						flag = this.QueueIncomingCommand(command);
						bool flag18 = flag;
						if (flag18)
						{
							EnetChannel channel = this.GetChannel(command.commandChannelID);
							bool flag19 = command.reliableSequenceNumber == command.startSequenceNumber;
							if (flag19)
							{
								command.fragmentsRemaining--;
								int num = command.startSequenceNumber + 1;
								while (command.fragmentsRemaining > 0 && num < command.startSequenceNumber + command.fragmentCount)
								{
									bool flag20 = channel.ContainsReliableSequenceNumber(num++);
									if (flag20)
									{
										command.fragmentsRemaining--;
									}
								}
							}
							else
							{
								bool flag21 = channel.ContainsReliableSequenceNumber(command.startSequenceNumber);
								if (flag21)
								{
									NCommand nCommand2 = channel.FetchReliableSequenceNumber(command.startSequenceNumber);
									nCommand2.fragmentsRemaining--;
								}
							}
						}
					}
				}
				break;
			}
			}
			return flag;
		}

		internal bool QueueIncomingCommand(NCommand command)
		{
			EnetChannel channel = this.GetChannel(command.commandChannelID);
			bool flag = channel == null;
			bool result;
			if (flag)
			{
				bool flag2 = base.debugOut >= DebugLevel.ERROR;
				if (flag2)
				{
					base.Listener.DebugReturn(DebugLevel.ERROR, "Received command for non-existing channel: " + command.commandChannelID);
				}
				result = false;
			}
			else
			{
				bool flag3 = base.debugOut >= DebugLevel.ALL;
				if (flag3)
				{
					base.Listener.DebugReturn(DebugLevel.ALL, string.Concat(new object[]
					{
						"queueIncomingCommand() ",
						command,
						" channel seq# r/u: ",
						channel.incomingReliableSequenceNumber,
						"/",
						channel.incomingUnreliableSequenceNumber
					}));
				}
				bool flag4 = command.commandFlags == 1;
				if (flag4)
				{
					bool flag5 = command.reliableSequenceNumber <= channel.incomingReliableSequenceNumber;
					if (flag5)
					{
						bool flag6 = base.debugOut >= DebugLevel.INFO;
						if (flag6)
						{
							base.Listener.DebugReturn(DebugLevel.INFO, string.Concat(new object[]
							{
								"incoming command ",
								command,
								" is old (not saving it). Dispatched incomingReliableSequenceNumber: ",
								channel.incomingReliableSequenceNumber
							}));
						}
						result = false;
					}
					else
					{
						bool flag7 = channel.ContainsReliableSequenceNumber(command.reliableSequenceNumber);
						if (flag7)
						{
							bool flag8 = base.debugOut >= DebugLevel.INFO;
							if (flag8)
							{
								base.Listener.DebugReturn(DebugLevel.INFO, string.Concat(new object[]
								{
									"Info: command was received before! Old/New: ",
									channel.FetchReliableSequenceNumber(command.reliableSequenceNumber),
									"/",
									command,
									" inReliableSeq#: ",
									channel.incomingReliableSequenceNumber
								}));
							}
							result = false;
						}
						else
						{
							channel.incomingReliableCommandsList.Add(command.reliableSequenceNumber, command);
							result = true;
						}
					}
				}
				else
				{
					bool flag9 = command.commandFlags == 0;
					if (flag9)
					{
						bool flag10 = command.reliableSequenceNumber < channel.incomingReliableSequenceNumber;
						if (flag10)
						{
							bool flag11 = base.debugOut >= DebugLevel.INFO;
							if (flag11)
							{
								base.Listener.DebugReturn(DebugLevel.INFO, "incoming reliable-seq# < Dispatched-rel-seq#. not saved.");
							}
							result = true;
						}
						else
						{
							bool flag12 = command.unreliableSequenceNumber <= channel.incomingUnreliableSequenceNumber;
							if (flag12)
							{
								bool flag13 = base.debugOut >= DebugLevel.INFO;
								if (flag13)
								{
									base.Listener.DebugReturn(DebugLevel.INFO, "incoming unreliable-seq# < Dispatched-unrel-seq#. not saved.");
								}
								result = true;
							}
							else
							{
								bool flag14 = channel.ContainsUnreliableSequenceNumber(command.unreliableSequenceNumber);
								if (flag14)
								{
									bool flag15 = base.debugOut >= DebugLevel.INFO;
									if (flag15)
									{
										base.Listener.DebugReturn(DebugLevel.INFO, string.Concat(new object[]
										{
											"command was received before! Old/New: ",
											channel.incomingUnreliableCommandsList[command.unreliableSequenceNumber],
											"/",
											command
										}));
									}
									result = false;
								}
								else
								{
									channel.incomingUnreliableCommandsList.Add(command.unreliableSequenceNumber, command);
									result = true;
								}
							}
						}
					}
					else
					{
						result = false;
					}
				}
			}
			return result;
		}

		internal NCommand RemoveSentReliableCommand(int ackReceivedReliableSequenceNumber, int ackReceivedChannel)
		{
			NCommand nCommand = null;
			List<NCommand> obj = this.sentReliableCommands;
			lock (obj)
			{
				foreach (NCommand current in this.sentReliableCommands)
				{
					bool flag = current != null && current.reliableSequenceNumber == ackReceivedReliableSequenceNumber && (int)current.commandChannelID == ackReceivedChannel;
					if (flag)
					{
						nCommand = current;
						break;
					}
				}
				bool flag2 = nCommand != null;
				if (flag2)
				{
					this.sentReliableCommands.Remove(nCommand);
					bool flag3 = this.sentReliableCommands.Count > 0;
					if (flag3)
					{
						this.timeoutInt = this.timeInt + 25;
					}
				}
				else
				{
					bool flag4 = base.debugOut >= DebugLevel.ALL && this.peerConnectionState != PeerBase.ConnectionStateValue.Connected && this.peerConnectionState != PeerBase.ConnectionStateValue.Disconnecting;
					if (flag4)
					{
						base.EnqueueDebugReturn(DebugLevel.ALL, string.Format("No sent command for ACK (Ch: {0} Sq#: {1}). PeerState: {2}.", ackReceivedReliableSequenceNumber, ackReceivedChannel, this.peerConnectionState));
					}
				}
			}
			return nCommand;
		}

		internal string CommandListToString(NCommand[] list)
		{
			bool flag = base.debugOut < DebugLevel.ALL;
			string result;
			if (flag)
			{
				result = string.Empty;
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < list.Length; i++)
				{
					stringBuilder.Append(i + "=");
					stringBuilder.Append(list[i]);
					stringBuilder.Append(" # ");
				}
				result = stringBuilder.ToString();
			}
			return result;
		}
	}
}
