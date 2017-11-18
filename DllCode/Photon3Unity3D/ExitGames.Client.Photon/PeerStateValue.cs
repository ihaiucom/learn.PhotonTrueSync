using System;

namespace ExitGames.Client.Photon
{
	public enum PeerStateValue : byte
	{
		Disconnected,
		Connecting,
		InitializingApplication = 10,
		Connected = 3,
		Disconnecting
	}
}
