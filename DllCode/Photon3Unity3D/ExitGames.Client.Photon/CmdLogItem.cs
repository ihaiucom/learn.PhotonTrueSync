using System;

namespace ExitGames.Client.Photon
{
	internal class CmdLogItem
	{
		public int TimeInt;

		public int Channel;

		public int SequenceNumber;

		public int Rtt;

		public int Variance;

		public CmdLogItem()
		{
		}

		public CmdLogItem(NCommand command, int timeInt, int rtt, int variance)
		{
			this.Channel = (int)command.commandChannelID;
			this.SequenceNumber = command.reliableSequenceNumber;
			this.TimeInt = timeInt;
			this.Rtt = rtt;
			this.Variance = variance;
		}

		public override string ToString()
		{
			return string.Format("NOW: {0,5}  CH: {1,3} SQ: {2,4} RTT: {3,4} VAR: {4,3}", new object[]
			{
				this.TimeInt,
				this.Channel,
				this.SequenceNumber,
				this.Rtt,
				this.Variance
			});
		}
	}
}
