using System;

namespace ExitGames.Client.Photon
{
	public class TrafficStatsGameLevel
	{
		private int timeOfLastDispatchCall;

		private int timeOfLastSendCall;

		public int OperationByteCount
		{
			get;
			set;
		}

		public int OperationCount
		{
			get;
			set;
		}

		public int ResultByteCount
		{
			get;
			set;
		}

		public int ResultCount
		{
			get;
			set;
		}

		public int EventByteCount
		{
			get;
			set;
		}

		public int EventCount
		{
			get;
			set;
		}

		public int LongestOpResponseCallback
		{
			get;
			set;
		}

		public byte LongestOpResponseCallbackOpCode
		{
			get;
			set;
		}

		public int LongestEventCallback
		{
			get;
			set;
		}

		public byte LongestEventCallbackCode
		{
			get;
			set;
		}

		public int LongestDeltaBetweenDispatching
		{
			get;
			set;
		}

		public int LongestDeltaBetweenSending
		{
			get;
			set;
		}

		[Obsolete("Use DispatchIncomingCommandsCalls, which has proper naming.")]
		public int DispatchCalls
		{
			get
			{
				return this.DispatchIncomingCommandsCalls;
			}
		}

		public int DispatchIncomingCommandsCalls
		{
			get;
			set;
		}

		public int SendOutgoingCommandsCalls
		{
			get;
			set;
		}

		public int TotalByteCount
		{
			get
			{
				return this.OperationByteCount + this.ResultByteCount + this.EventByteCount;
			}
		}

		public int TotalMessageCount
		{
			get
			{
				return this.OperationCount + this.ResultCount + this.EventCount;
			}
		}

		public int TotalIncomingByteCount
		{
			get
			{
				return this.ResultByteCount + this.EventByteCount;
			}
		}

		public int TotalIncomingMessageCount
		{
			get
			{
				return this.ResultCount + this.EventCount;
			}
		}

		public int TotalOutgoingByteCount
		{
			get
			{
				return this.OperationByteCount;
			}
		}

		public int TotalOutgoingMessageCount
		{
			get
			{
				return this.OperationCount;
			}
		}

		internal void CountOperation(int operationBytes)
		{
			this.OperationByteCount += operationBytes;
			int operationCount = this.OperationCount;
			this.OperationCount = operationCount + 1;
		}

		internal void CountResult(int resultBytes)
		{
			this.ResultByteCount += resultBytes;
			int resultCount = this.ResultCount;
			this.ResultCount = resultCount + 1;
		}

		internal void CountEvent(int eventBytes)
		{
			this.EventByteCount += eventBytes;
			int eventCount = this.EventCount;
			this.EventCount = eventCount + 1;
		}

		internal void TimeForResponseCallback(byte code, int time)
		{
			bool flag = time > this.LongestOpResponseCallback;
			if (flag)
			{
				this.LongestOpResponseCallback = time;
				this.LongestOpResponseCallbackOpCode = code;
			}
		}

		internal void TimeForEventCallback(byte code, int time)
		{
			bool flag = time > this.LongestEventCallback;
			if (flag)
			{
				this.LongestEventCallback = time;
				this.LongestEventCallbackCode = code;
			}
		}

		internal void DispatchIncomingCommandsCalled()
		{
			bool flag = this.timeOfLastDispatchCall != 0;
			if (flag)
			{
				int num = SupportClass.GetTickCount() - this.timeOfLastDispatchCall;
				bool flag2 = num > this.LongestDeltaBetweenDispatching;
				if (flag2)
				{
					this.LongestDeltaBetweenDispatching = num;
				}
			}
			int dispatchIncomingCommandsCalls = this.DispatchIncomingCommandsCalls;
			this.DispatchIncomingCommandsCalls = dispatchIncomingCommandsCalls + 1;
			this.timeOfLastDispatchCall = SupportClass.GetTickCount();
		}

		internal void SendOutgoingCommandsCalled()
		{
			bool flag = this.timeOfLastSendCall != 0;
			if (flag)
			{
				int num = SupportClass.GetTickCount() - this.timeOfLastSendCall;
				bool flag2 = num > this.LongestDeltaBetweenSending;
				if (flag2)
				{
					this.LongestDeltaBetweenSending = num;
				}
			}
			int sendOutgoingCommandsCalls = this.SendOutgoingCommandsCalls;
			this.SendOutgoingCommandsCalls = sendOutgoingCommandsCalls + 1;
			this.timeOfLastSendCall = SupportClass.GetTickCount();
		}

		public void ResetMaximumCounters()
		{
			this.LongestDeltaBetweenDispatching = 0;
			this.LongestDeltaBetweenSending = 0;
			this.LongestEventCallback = 0;
			this.LongestEventCallbackCode = 0;
			this.LongestOpResponseCallback = 0;
			this.LongestOpResponseCallbackOpCode = 0;
			this.timeOfLastDispatchCall = 0;
			this.timeOfLastSendCall = 0;
		}

		public override string ToString()
		{
			return string.Format("OperationByteCount: {0} ResultByteCount: {1} EventByteCount: {2}", this.OperationByteCount, this.ResultByteCount, this.EventByteCount);
		}

		public string ToStringVitalStats()
		{
			return string.Format("Longest delta between Send: {0}ms Dispatch: {1}ms. Longest callback OnEv: {3}={2}ms OnResp: {5}={4}ms. Calls of Send: {6} Dispatch: {7}.", new object[]
			{
				this.LongestDeltaBetweenSending,
				this.LongestDeltaBetweenDispatching,
				this.LongestEventCallback,
				this.LongestEventCallbackCode,
				this.LongestOpResponseCallback,
				this.LongestOpResponseCallbackOpCode,
				this.SendOutgoingCommandsCalls,
				this.DispatchIncomingCommandsCalls
			});
		}
	}
}
