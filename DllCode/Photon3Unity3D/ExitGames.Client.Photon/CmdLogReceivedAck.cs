using System;

namespace ExitGames.Client.Photon
{
	internal class CmdLogReceivedAck : CmdLogItem
	{
		public int ReceivedSentTime;

		public CmdLogReceivedAck(NCommand command, int timeInt, int rtt, int variance)
		{
			this.TimeInt = timeInt;
			this.Channel = (int)command.commandChannelID;
			this.SequenceNumber = command.ackReceivedReliableSequenceNumber;
			this.Rtt = rtt;
			this.Variance = variance;
			this.ReceivedSentTime = command.ackReceivedSentTime;
		}

		public override string ToString()
		{
			return string.Format("ACK  NOW: {0,5}  CH: {1,3} SQ: {2,4} RTT: {3,4} VAR: {4,3}  Sent: {5,5} Diff: {6,4}", new object[]
			{
				this.TimeInt,
				this.Channel,
				this.SequenceNumber,
				this.Rtt,
				this.Variance,
				this.ReceivedSentTime,
				this.TimeInt - this.ReceivedSentTime
			});
		}
	}
}
