using System;

namespace ExitGames.Client.Photon
{
	public enum ConnectionProtocol : byte
	{
		Udp,
		Tcp,
		WebSocket = 4,
		WebSocketSecure
	}
}
