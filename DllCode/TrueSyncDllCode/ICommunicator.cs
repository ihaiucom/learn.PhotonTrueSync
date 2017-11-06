using System;

public interface ICommunicator
{
	int RoundTripTime();

	void OpRaiseEvent(byte eventCode, object message, bool reliable, int[] toPlayers);

	void AddEventListener(OnEventReceived onEventReceived);
}
