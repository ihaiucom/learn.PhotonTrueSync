using System;

namespace ExitGames.Client.Photon
{
	internal class CmdLogReceivedReliable : CmdLogItem
	{
		public int TimeSinceLastSend;

		public int TimeSinceLastSendAck;

		public CmdLogReceivedReliable(NCommand command, int timeInt, int rtt, int variance, int timeSinceLastSend, int timeSinceLastSendAck) : base(command, timeInt, rtt, variance)
		{
			this.TimeSinceLastSend = timeSinceLastSend;
			this.TimeSinceLastSendAck = timeSinceLastSendAck;
		}

		public override string ToString()
		{
			return string.Format("Read reliable. {0}  TimeSinceLastSend: {1} TimeSinceLastSendAcks: {2}", base.ToString(), this.TimeSinceLastSend, this.TimeSinceLastSendAck);
		}
	}
}
