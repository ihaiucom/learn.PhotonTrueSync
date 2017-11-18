using System;

namespace ExitGames.Client.Photon
{
	internal class NCommand : IComparable<NCommand>
	{
		internal byte commandFlags;

		internal const int FLAG_RELIABLE = 1;

		internal const int FLAG_UNSEQUENCED = 2;

		internal const byte FV_UNRELIABLE = 0;

		internal const byte FV_RELIABLE = 1;

		internal const byte FV_UNRELIBALE_UNSEQUENCED = 2;

		internal byte commandType;

		internal const byte CT_NONE = 0;

		internal const byte CT_ACK = 1;

		internal const byte CT_CONNECT = 2;

		internal const byte CT_VERIFYCONNECT = 3;

		internal const byte CT_DISCONNECT = 4;

		internal const byte CT_PING = 5;

		internal const byte CT_SENDRELIABLE = 6;

		internal const byte CT_SENDUNRELIABLE = 7;

		internal const byte CT_SENDFRAGMENT = 8;

		internal const byte CT_EG_SERVERTIME = 12;

		internal byte commandChannelID;

		internal int reliableSequenceNumber;

		internal int unreliableSequenceNumber;

		internal int unsequencedGroupNumber;

		internal byte reservedByte = 4;

		internal int startSequenceNumber;

		internal int fragmentCount;

		internal int fragmentNumber;

		internal int totalLength;

		internal int fragmentOffset;

		internal int fragmentsRemaining;

		internal int commandSentTime;

		internal byte commandSentCount;

		internal int roundTripTimeout;

		internal int timeoutTime;

		internal int ackReceivedReliableSequenceNumber;

		internal int ackReceivedSentTime;

		internal const int HEADER_UDP_PACK_LENGTH = 12;

		internal const int CmdSizeMinimum = 12;

		internal const int CmdSizeAck = 20;

		internal const int CmdSizeConnect = 44;

		internal const int CmdSizeVerifyConnect = 44;

		internal const int CmdSizeDisconnect = 12;

		internal const int CmdSizePing = 12;

		internal const int CmdSizeReliableHeader = 12;

		internal const int CmdSizeUnreliableHeader = 16;

		internal const int CmdSizeFragmentHeader = 32;

		internal const int CmdSizeMaxHeader = 36;

		internal int Size;

		private byte[] commandHeader;

		internal int SizeOfHeader;

		internal byte[] Payload;

		protected internal int SizeOfPayload
		{
			get
			{
				return (this.Payload != null) ? this.Payload.Length : 0;
			}
		}

		internal NCommand(EnetPeer peer, byte commandType, byte[] payload, byte channel)
		{
			this.commandType = commandType;
			this.commandFlags = 1;
			this.commandChannelID = channel;
			this.Payload = payload;
			this.Size = 12;
			switch (this.commandType)
			{
			case 2:
			{
				this.Size = 44;
				this.Payload = new byte[32];
				this.Payload[0] = 0;
				this.Payload[1] = 0;
				int num = 2;
				Protocol.Serialize((short)peer.mtu, this.Payload, ref num);
				this.Payload[4] = 0;
				this.Payload[5] = 0;
				this.Payload[6] = 128;
				this.Payload[7] = 0;
				this.Payload[11] = peer.ChannelCount;
				this.Payload[15] = 0;
				this.Payload[19] = 0;
				this.Payload[22] = 19;
				this.Payload[23] = 136;
				this.Payload[27] = 2;
				this.Payload[31] = 2;
				break;
			}
			case 4:
			{
				this.Size = 12;
				bool flag = peer.peerConnectionState != PeerBase.ConnectionStateValue.Connected;
				if (flag)
				{
					this.commandFlags = 2;
					bool flag2 = peer.peerConnectionState == PeerBase.ConnectionStateValue.Zombie;
					if (flag2)
					{
						this.reservedByte = 2;
					}
				}
				break;
			}
			case 6:
				this.Size = 12 + payload.Length;
				break;
			case 7:
				this.Size = 16 + payload.Length;
				this.commandFlags = 0;
				break;
			case 8:
				this.Size = 32 + payload.Length;
				break;
			}
		}

		internal static void CreateAck(byte[] buffer, int offset, NCommand commandToAck, int sentTime)
		{
			buffer[offset++] = 1;
			buffer[offset++] = commandToAck.commandChannelID;
			buffer[offset++] = 0;
			buffer[offset++] = commandToAck.reservedByte;
			Protocol.Serialize(20, buffer, ref offset);
			Protocol.Serialize(commandToAck.reliableSequenceNumber, buffer, ref offset);
			Protocol.Serialize(commandToAck.reliableSequenceNumber, buffer, ref offset);
			Protocol.Serialize(sentTime, buffer, ref offset);
		}

		internal NCommand(EnetPeer peer, byte[] inBuff, ref int readingOffset)
		{
			int num = readingOffset;
			readingOffset = num + 1;
			this.commandType = inBuff[num];
			num = readingOffset;
			readingOffset = num + 1;
			this.commandChannelID = inBuff[num];
			num = readingOffset;
			readingOffset = num + 1;
			this.commandFlags = inBuff[num];
			num = readingOffset;
			readingOffset = num + 1;
			this.reservedByte = inBuff[num];
			Protocol.Deserialize(out this.Size, inBuff, ref readingOffset);
			Protocol.Deserialize(out this.reliableSequenceNumber, inBuff, ref readingOffset);
			peer.bytesIn += (long)this.Size;
			switch (this.commandType)
			{
			case 1:
				Protocol.Deserialize(out this.ackReceivedReliableSequenceNumber, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.ackReceivedSentTime, inBuff, ref readingOffset);
				break;
			case 3:
			{
				short peerID;
				Protocol.Deserialize(out peerID, inBuff, ref readingOffset);
				readingOffset += 30;
				bool flag = peer.peerID == -1 || peer.peerID == -2;
				if (flag)
				{
					peer.peerID = peerID;
				}
				break;
			}
			case 6:
				this.Payload = new byte[this.Size - 12];
				break;
			case 7:
				Protocol.Deserialize(out this.unreliableSequenceNumber, inBuff, ref readingOffset);
				this.Payload = new byte[this.Size - 16];
				break;
			case 8:
				Protocol.Deserialize(out this.startSequenceNumber, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.fragmentCount, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.fragmentNumber, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.totalLength, inBuff, ref readingOffset);
				Protocol.Deserialize(out this.fragmentOffset, inBuff, ref readingOffset);
				this.Payload = new byte[this.Size - 32];
				this.fragmentsRemaining = this.fragmentCount;
				break;
			}
			bool flag2 = this.Payload != null;
			if (flag2)
			{
				Buffer.BlockCopy(inBuff, readingOffset, this.Payload, 0, this.Payload.Length);
				readingOffset += this.Payload.Length;
			}
		}

		internal void SerializeHeader(byte[] buffer, ref int bufferIndex)
		{
			bool flag = this.commandHeader != null;
			if (!flag)
			{
				this.SizeOfHeader = 12;
				bool flag2 = this.commandType == 7;
				if (flag2)
				{
					this.SizeOfHeader = 16;
				}
				else
				{
					bool flag3 = this.commandType == 8;
					if (flag3)
					{
						this.SizeOfHeader = 32;
					}
				}
				int num = bufferIndex;
				bufferIndex = num + 1;
				buffer[num] = this.commandType;
				num = bufferIndex;
				bufferIndex = num + 1;
				buffer[num] = this.commandChannelID;
				num = bufferIndex;
				bufferIndex = num + 1;
				buffer[num] = this.commandFlags;
				num = bufferIndex;
				bufferIndex = num + 1;
				buffer[num] = this.reservedByte;
				Protocol.Serialize(this.Size, buffer, ref bufferIndex);
				Protocol.Serialize(this.reliableSequenceNumber, buffer, ref bufferIndex);
				bool flag4 = this.commandType == 7;
				if (flag4)
				{
					Protocol.Serialize(this.unreliableSequenceNumber, buffer, ref bufferIndex);
				}
				else
				{
					bool flag5 = this.commandType == 8;
					if (flag5)
					{
						Protocol.Serialize(this.startSequenceNumber, buffer, ref bufferIndex);
						Protocol.Serialize(this.fragmentCount, buffer, ref bufferIndex);
						Protocol.Serialize(this.fragmentNumber, buffer, ref bufferIndex);
						Protocol.Serialize(this.totalLength, buffer, ref bufferIndex);
						Protocol.Serialize(this.fragmentOffset, buffer, ref bufferIndex);
					}
				}
			}
		}

		internal byte[] Serialize()
		{
			return this.Payload;
		}

		public int CompareTo(NCommand other)
		{
			bool flag = (this.commandFlags & 1) > 0;
			int result;
			if (flag)
			{
				result = this.reliableSequenceNumber - other.reliableSequenceNumber;
			}
			else
			{
				result = this.unreliableSequenceNumber - other.unreliableSequenceNumber;
			}
			return result;
		}

		public override string ToString()
		{
			bool flag = this.commandType == 1;
			string result;
			if (flag)
			{
				result = string.Format("CMD({1} ack for c#:{0} s#/time {2}/{3})", new object[]
				{
					this.commandChannelID,
					this.commandType,
					this.ackReceivedReliableSequenceNumber,
					this.ackReceivedSentTime
				});
			}
			else
			{
				result = string.Format("CMD({1} c#:{0} r/u: {2}/{3} st/r#/rt:{4}/{5}/{6})", new object[]
				{
					this.commandChannelID,
					this.commandType,
					this.reliableSequenceNumber,
					this.unreliableSequenceNumber,
					this.commandSentTime,
					this.commandSentCount,
					this.timeoutTime
				});
			}
			return result;
		}
	}
}
