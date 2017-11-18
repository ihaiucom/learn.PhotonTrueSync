using System;

namespace ExitGames.Client.Photon
{
	public enum StatusCode
	{
		Connect = 1024,
		Disconnect,
		Exception,
		ExceptionOnConnect = 1023,
		SecurityExceptionOnConnect = 1022,
		[Obsolete("Check QueuedOutgoingCommands and QueuedIncomingCommands on demand instead.")]
		QueueOutgoingReliableWarning = 1027,
		[Obsolete("Check QueuedOutgoingCommands and QueuedIncomingCommands on demand instead.")]
		QueueOutgoingUnreliableWarning = 1029,
		SendError,
		[Obsolete("Check QueuedOutgoingCommands and QueuedIncomingCommands on demand instead.")]
		QueueOutgoingAcksWarning,
		[Obsolete("Check QueuedOutgoingCommands and QueuedIncomingCommands on demand instead.")]
		QueueIncomingReliableWarning = 1033,
		[Obsolete("Check QueuedOutgoingCommands and QueuedIncomingCommands on demand instead.")]
		QueueIncomingUnreliableWarning = 1035,
		[Obsolete("Check QueuedOutgoingCommands and QueuedIncomingCommands on demand instead.")]
		QueueSentWarning = 1037,
		ExceptionOnReceive = 1039,
		TimeoutDisconnect,
		DisconnectByServer,
		DisconnectByServerUserLimit,
		DisconnectByServerLogic,
		EncryptionEstablished = 1048,
		EncryptionFailedToEstablish
	}
}
