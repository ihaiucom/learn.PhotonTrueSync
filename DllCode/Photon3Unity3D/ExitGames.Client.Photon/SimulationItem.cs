using System;
using System.Diagnostics;

namespace ExitGames.Client.Photon
{
	internal class SimulationItem
	{
		internal readonly Stopwatch stopw;

		public int TimeToExecute;

		public PeerBase.MyAction ActionToExecute;

		public int Delay
		{
			get;
			internal set;
		}

		public SimulationItem()
		{
			this.stopw = new Stopwatch();
			this.stopw.Start();
		}
	}
}
