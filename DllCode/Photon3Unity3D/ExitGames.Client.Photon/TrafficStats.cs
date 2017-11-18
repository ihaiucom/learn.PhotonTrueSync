using System;

namespace ExitGames.Client.Photon
{
	public class TrafficStats
	{
		public int PackageHeaderSize
		{
			get;
			internal set;
		}

		public int ReliableCommandCount
		{
			get;
			internal set;
		}

		public int UnreliableCommandCount
		{
			get;
			internal set;
		}

		public int FragmentCommandCount
		{
			get;
			internal set;
		}

		public int ControlCommandCount
		{
			get;
			internal set;
		}

		public int TotalPacketCount
		{
			get;
			internal set;
		}

		public int TotalCommandsInPackets
		{
			get;
			internal set;
		}

		public int ReliableCommandBytes
		{
			get;
			internal set;
		}

		public int UnreliableCommandBytes
		{
			get;
			internal set;
		}

		public int FragmentCommandBytes
		{
			get;
			internal set;
		}

		public int ControlCommandBytes
		{
			get;
			internal set;
		}

		public int TotalCommandCount
		{
			get
			{
				return this.ReliableCommandCount + this.UnreliableCommandCount + this.FragmentCommandCount + this.ControlCommandCount;
			}
		}

		public int TotalCommandBytes
		{
			get
			{
				return this.ReliableCommandBytes + this.UnreliableCommandBytes + this.FragmentCommandBytes + this.ControlCommandBytes;
			}
		}

		public int TotalPacketBytes
		{
			get
			{
				return this.TotalCommandBytes + this.TotalPacketCount * this.PackageHeaderSize;
			}
		}

		public int TimestampOfLastAck
		{
			get;
			set;
		}

		public int TimestampOfLastReliableCommand
		{
			get;
			set;
		}

		internal TrafficStats(int packageHeaderSize)
		{
			this.PackageHeaderSize = packageHeaderSize;
		}

		internal void CountControlCommand(int size)
		{
			this.ControlCommandBytes += size;
			int controlCommandCount = this.ControlCommandCount;
			this.ControlCommandCount = controlCommandCount + 1;
		}

		internal void CountReliableOpCommand(int size)
		{
			this.ReliableCommandBytes += size;
			int reliableCommandCount = this.ReliableCommandCount;
			this.ReliableCommandCount = reliableCommandCount + 1;
		}

		internal void CountUnreliableOpCommand(int size)
		{
			this.UnreliableCommandBytes += size;
			int unreliableCommandCount = this.UnreliableCommandCount;
			this.UnreliableCommandCount = unreliableCommandCount + 1;
		}

		internal void CountFragmentOpCommand(int size)
		{
			this.FragmentCommandBytes += size;
			int fragmentCommandCount = this.FragmentCommandCount;
			this.FragmentCommandCount = fragmentCommandCount + 1;
		}

		public override string ToString()
		{
			return string.Format("TotalPacketBytes: {0} TotalCommandBytes: {1} TotalPacketCount: {2} TotalCommandsInPackets: {3}", new object[]
			{
				this.TotalPacketBytes,
				this.TotalCommandBytes,
				this.TotalPacketCount,
				this.TotalCommandsInPackets
			});
		}
	}
}
