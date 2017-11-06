using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrueSync
{
	public abstract class AbstractLockstep
	{
		private enum SimulationState
		{
			NOT_STARTED,
			WAITING_PLAYERS,
			RUNNING,
			PAUSED,
			ENDED
		}

		private const int INITIAL_PLAYERS_CAPACITY = 4;

		private const byte SYNCED_GAME_START_CODE = 196;

		private const byte SIMULATION_CODE = 197;

		private const byte CHECKSUM_CODE = 198;

		private const byte SEND_CODE = 199;

		private const byte SIMULATION_EVENT_PAUSE = 0;

		private const byte SIMULATION_EVENT_RUN = 1;

		private const byte SIMULATION_EVENT_END = 3;

		private const int MAX_PANIC_BEFORE_END_GAME = 5;

		private const int SYNCED_INFO_BUFFER_WINDOW = 3;

		internal Dictionary<byte, TSPlayer> players;

		internal List<TSPlayer> activePlayers;

		internal List<SyncedData> auxPlayersSyncedData;

		internal List<InputDataBase> auxPlayersInputData;

		internal int[] auxActivePlayersIds;

		internal TSPlayer localPlayer;

		protected TrueSyncUpdateCallback StepUpdate;

		private TrueSyncInputCallback GetLocalData;

		internal TrueSyncInputDataProvider InputDataProvider;

		private TrueSyncEventCallback OnGameStarted;

		private TrueSyncEventCallback OnGamePaused;

		private TrueSyncEventCallback OnGameUnPaused;

		private TrueSyncEventCallback OnGameEnded;

		private TrueSyncPlayerDisconnectionCallback OnPlayerDisconnection;

		public TrueSyncIsReady GameIsReady;

		protected int ticks;

		private int panicWindow;

		protected int syncWindow;

		private int elapsedPanicTicks;

		private AbstractLockstep.SimulationState simulationState;

		internal int rollbackWindow;

		internal ICommunicator communicator;

		protected IPhysicsManagerBase physicsManager;

		private GenericBufferWindow<SyncedInfo> bufferSyncedInfo;

		protected int totalWindow;

		public bool checksumOk;

		public CompoundStats compoundStats;

		public float deltaTime;

		public int _lastSafeTick = 0;

		protected Dictionary<int, List<IBody>> bodiesToDestroy;

        // <this.GetSyncedDataTick() + 1, List<PlayerID>>
        protected Dictionary<int, List<byte>> playersDisconnect;

		private ReplayMode replayMode;

		private ReplayRecord replayRecord;

		internal static AbstractLockstep instance;

		private List<int> playersIdsAux = new List<int>();

		private SyncedData[] _syncedDataCacheDrop = new SyncedData[1];

		private SyncedData[] _syncedDataCacheUpdateData = new SyncedData[1];

		public List<TSPlayer> ActivePlayers
		{
			get
			{
				return this.activePlayers;
			}
		}

		public IDictionary<byte, TSPlayer> Players
		{
			get
			{
				return this.players;
			}
		}

		public TSPlayer LocalPlayer
		{
			get
			{
				return this.localPlayer;
			}
		}

		public int Ticks
		{
			get
			{
				return this.GetSimulatedTick(this.GetSyncedDataTick()) - 1;
			}
		}

		public int LastSafeTick
		{
			get
			{
				bool flag = this._lastSafeTick < 0;
				int result;
				if (flag)
				{
					result = -1;
				}
				else
				{
					result = this._lastSafeTick - 1;
				}
				return result;
			}
		}

		private ReplayMode ReplayMode
		{
			set
			{
				this.replayMode = value;
				bool flag = this.replayMode == ReplayMode.RECORD_REPLAY;
				if (flag)
				{
					this.replayRecord = new ReplayRecord();
				}
			}
		}

		public ReplayRecord ReplayRecord
		{
			set
			{
				this.replayRecord = value;
				bool flag = this.replayRecord != null;
				if (flag)
				{
					this.replayMode = ReplayMode.LOAD_REPLAY;
					this.replayRecord.ApplyRecord(this);
				}
			}
		}

		public static AbstractLockstep NewInstance(float deltaTime, ICommunicator communicator, IPhysicsManagerBase physicsManager, int syncWindow, int panicWindow, int rollbackWindow, TrueSyncEventCallback OnGameStarted, TrueSyncEventCallback OnGamePaused, TrueSyncEventCallback OnGameUnPaused, TrueSyncEventCallback OnGameEnded, TrueSyncPlayerDisconnectionCallback OnPlayerDisconnection, TrueSyncUpdateCallback OnStepUpdate, TrueSyncInputCallback GetLocalData, TrueSyncInputDataProvider InputDataProvider)
		{
			bool flag = rollbackWindow <= 0 || communicator == null;
			AbstractLockstep result;
			if (flag)
			{
				result = new DefaultLockstep(deltaTime, communicator, physicsManager, syncWindow, panicWindow, rollbackWindow, OnGameStarted, OnGamePaused, OnGameUnPaused, OnGameEnded, OnPlayerDisconnection, OnStepUpdate, GetLocalData, InputDataProvider);
			}
			else
			{
				result = new RollbackLockstep(deltaTime, communicator, physicsManager, syncWindow, panicWindow, rollbackWindow, OnGameStarted, OnGamePaused, OnGameUnPaused, OnGameEnded, OnPlayerDisconnection, OnStepUpdate, GetLocalData, InputDataProvider);
			}
			return result;
		}

		public AbstractLockstep(float deltaTime, ICommunicator communicator, IPhysicsManagerBase physicsManager, int syncWindow, int panicWindow, int rollbackWindow, TrueSyncEventCallback OnGameStarted, TrueSyncEventCallback OnGamePaused, TrueSyncEventCallback OnGameUnPaused, TrueSyncEventCallback OnGameEnded, TrueSyncPlayerDisconnectionCallback OnPlayerDisconnection, TrueSyncUpdateCallback OnStepUpdate, TrueSyncInputCallback GetLocalData, TrueSyncInputDataProvider InputDataProvider)
		{
			AbstractLockstep.instance = this;
			this.deltaTime = deltaTime;
			this.syncWindow = syncWindow;
			this.panicWindow = panicWindow;
			this.rollbackWindow = rollbackWindow;
			this.totalWindow = syncWindow + rollbackWindow;
			this.StepUpdate = OnStepUpdate;
			this.OnGameStarted = OnGameStarted;
			this.OnGamePaused = OnGamePaused;
			this.OnGameUnPaused = OnGameUnPaused;
			this.OnGameEnded = OnGameEnded;
			this.OnPlayerDisconnection = OnPlayerDisconnection;
			this.GetLocalData = GetLocalData;
			this.InputDataProvider = InputDataProvider;
			this.ticks = 0;
			this.players = new Dictionary<byte, TSPlayer>(4);
			this.activePlayers = new List<TSPlayer>(4);
			this.auxPlayersSyncedData = new List<SyncedData>(4);
			this.auxPlayersInputData = new List<InputDataBase>(4);
			this.communicator = communicator;
			bool flag = communicator != null;
			if (flag)
			{
				this.communicator.AddEventListener(new OnEventReceived(this.OnEventDataReceived));
			}
			this.physicsManager = physicsManager;
			this.compoundStats = new CompoundStats();
			this.bufferSyncedInfo = new GenericBufferWindow<SyncedInfo>(3);
			this.checksumOk = true;
			this.simulationState = AbstractLockstep.SimulationState.NOT_STARTED;
			this.bodiesToDestroy = new Dictionary<int, List<IBody>>();
			this.playersDisconnect = new Dictionary<int, List<byte>>();
			this.ReplayMode = ReplayRecord.replayMode;
		}

		protected int GetSyncedDataTick()
		{
			return this.ticks - this.syncWindow;
		}

		protected abstract int GetRefTick(int syncedDataTick);

		protected virtual void BeforeStepUpdate(int syncedDataTick, int referenceTick)
		{
		}

		protected virtual void AfterStepUpdate(int syncedDataTick, int referenceTick)
		{
			int i = 0;
			int count = this.activePlayers.Count;
			while (i < count)
			{
				this.activePlayers[i].RemoveData(referenceTick);
				i++;
			}
		}

		protected abstract bool IsStepReady(int syncedDataTick);

		protected abstract void OnSyncedDataReceived(TSPlayer player, List<SyncedData> data);

		protected abstract string GetChecksumForSyncedInfo();

		protected abstract int GetSimulatedTick(int syncedDataTick);

		private void Run()
		{
			bool flag = this.simulationState == AbstractLockstep.SimulationState.NOT_STARTED;
			if (flag)
			{
				this.simulationState = AbstractLockstep.SimulationState.WAITING_PLAYERS;
			}
			else
			{
				bool flag2 = this.simulationState == AbstractLockstep.SimulationState.WAITING_PLAYERS || this.simulationState == AbstractLockstep.SimulationState.PAUSED;
				if (flag2)
				{
					bool flag3 = this.simulationState == AbstractLockstep.SimulationState.WAITING_PLAYERS;
					if (flag3)
					{
						this.OnGameStarted();
					}
					else
					{
						this.OnGameUnPaused();
					}
					this.simulationState = AbstractLockstep.SimulationState.RUNNING;
				}
			}
		}

		private void Pause()
		{
			bool flag = this.simulationState == AbstractLockstep.SimulationState.RUNNING;
			if (flag)
			{
				this.OnGamePaused();
				this.simulationState = AbstractLockstep.SimulationState.PAUSED;
			}
		}

		private void End()
		{
			bool flag = this.simulationState != AbstractLockstep.SimulationState.ENDED;
			if (flag)
			{
				this.OnGameEnded();
				bool flag2 = this.replayMode == ReplayMode.RECORD_REPLAY;
				if (flag2)
				{
					ReplayRecord.SaveRecord(this.replayRecord);
				}
				this.simulationState = AbstractLockstep.SimulationState.ENDED;
			}
		}

		public void Update()
		{
			bool flag = this.simulationState == AbstractLockstep.SimulationState.WAITING_PLAYERS;
			if (flag)
			{
				this.CheckGameStart();
			}
			else
			{
				bool flag2 = this.simulationState == AbstractLockstep.SimulationState.RUNNING;
				if (flag2)
				{
                    // 复合状态信息 更新时间，循环更新buff当前值
					this.compoundStats.UpdateTime(this.deltaTime);
					bool flag3 = this.communicator != null;
					if (flag3)
					{
                        // 设置当前ping
						this.compoundStats.AddValue("ping", (long)this.communicator.RoundTripTime());
					}

                    // 如果同步窗口为0
					bool flag4 = this.syncWindow == 0;
					if (flag4)
					{
                        // 如果不是录像模式；获取本地玩家的输入操作，添加到本地玩家的当前帧同步数据;发送本地玩家操作给其他玩家
                        this.UpdateData();
					}
                    //  检测玩家掉线
					int i = 0;
					int num = this.activePlayers.Count;
					while (i < num)
					{
						bool flag5 = this.CheckDrop(this.activePlayers[i]);
						if (flag5)
						{
							i--;
							num--;
						}
						i++;
					}

                    // 检测帧是否准备完毕
					int syncedDataTick = this.GetSyncedDataTick();
					bool flag6 = this.CheckGameIsReady() && this.IsStepReady(syncedDataTick);
					bool flag7 = flag6;
					if (flag7)
					{
                        // 统计 的帧 +1
						this.compoundStats.Increment("simulated_frames");

                        // 如果不是录像模式；获取本地玩家的输入操作，添加到本地玩家的当前帧同步数据;发送本地玩家操作给其他玩家
                        this.UpdateData();
                        // 将过去恐慌帧设置为0
						this.elapsedPanicTicks = 0;
                        // 获取参考帧
						int refTick = this.GetRefTick(syncedDataTick);
						bool flag8 = refTick > 1 && refTick % 100 == 0;
						if (flag8)
						{
                            // 每100帧调一次, 发送校验码
							this.SendInfoChecksum(refTick);
						}
                        // 设置安全帧
						this._lastSafeTick = refTick;
                        // 帧更新前调用
						this.BeforeStepUpdate(syncedDataTick, refTick);
						List<SyncedData> tickData = this.GetTickData(syncedDataTick);
						this.ExecutePhysicsStep(tickData, syncedDataTick);
						bool flag9 = this.replayMode == ReplayMode.RECORD_REPLAY;
						if (flag9)
						{
							this.replayRecord.AddSyncedData(this.GetTickData(refTick));
						}
						this.AfterStepUpdate(syncedDataTick, refTick);
						this.ticks++;
					}
					else
					{
						bool flag10 = this.ticks >= this.totalWindow;
						if (flag10)
						{
							bool flag11 = this.replayMode == ReplayMode.LOAD_REPLAY;
							if (flag11)
							{
								this.End();
							}
							else
							{
								this.compoundStats.Increment("missed_frames");
								this.elapsedPanicTicks++;
								bool flag12 = this.elapsedPanicTicks > this.panicWindow;
								if (flag12)
								{
									this.compoundStats.Increment("panic");
									bool flag13 = this.compoundStats.globalStats.GetInfo("panic").count >= 5L;
									if (flag13)
									{
										this.End();
									}
									else
									{
										this.elapsedPanicTicks = 0;
										this.DropLagPlayers();
									}
								}
							}
						}
						else
						{
							this.compoundStats.Increment("simulated_frames");
							this.physicsManager.UpdateStep();
							this.UpdateData();
							this.ticks++;
						}
					}
				}
			}
		}

        // 检测游戏是否准备完成
		private bool CheckGameIsReady()
		{
			bool flag = this.GameIsReady != null;
			bool result;
			if (flag)
			{
				Delegate[] invocationList = this.GameIsReady.GetInvocationList();
				for (int i = 0; i < invocationList.Length; i++)
				{
					Delegate @delegate = invocationList[i];
					bool flag2 = (bool)@delegate.DynamicInvoke(new object[0]);
					bool flag3 = !flag2;
					if (flag3)
					{
						result = false;
						return result;
					}
				}
			}
			result = true;
			return result;
		}

		protected void ExecutePhysicsStep(List<SyncedData> data, int syncedDataTick)
		{
			this.ExecuteDelegates(syncedDataTick);
			this.SyncedArrayToInputArray(data);
			this.StepUpdate(this.auxPlayersInputData);
			this.physicsManager.UpdateStep();
		}

		private void ExecuteDelegates(int syncedDataTick)
		{
			syncedDataTick++;
			bool flag = this.playersDisconnect.ContainsKey(syncedDataTick);
			if (flag)
			{
				List<byte> list = this.playersDisconnect[syncedDataTick];
				int i = 0;
				int count = list.Count;
				while (i < count)
				{
					this.OnPlayerDisconnection(list[i]);
					i++;
				}
			}
		}

		internal void UpdateActivePlayers()
		{
			this.playersIdsAux.Clear();
			int i = 0;
			int count = this.activePlayers.Count;
			while (i < count)
			{
				bool flag = this.localPlayer == null || this.localPlayer.ID != this.activePlayers[i].ID;
				if (flag)
				{
					this.playersIdsAux.Add((int)this.activePlayers[i].ID);
				}
				i++;
			}
			this.auxActivePlayersIds = this.playersIdsAux.ToArray();
		}

		private void CheckGameStart()
		{
			bool flag = this.replayMode == ReplayMode.LOAD_REPLAY;
			if (flag)
			{
				this.RunSimulation(false);
			}
			else
			{
				bool flag2 = true;
				int i = 0;
				int count = this.activePlayers.Count;
				while (i < count)
				{
					flag2 &= this.activePlayers[i].sentSyncedStart;
					i++;
				}
				bool flag3 = flag2;
				if (flag3)
				{
					this.RunSimulation(false);
					SyncedData.pool.FillStack(this.activePlayers.Count * (this.syncWindow + this.rollbackWindow));
				}
				else
				{
                    // 196
					this.RaiseEvent(SYNCED_GAME_START_CODE, SyncedInfo.Encode(new SyncedInfo
					{
						playerId = this.localPlayer.ID
					}));
				}
			}
		}

		protected void SyncedArrayToInputArray(List<SyncedData> data)
		{
			this.auxPlayersInputData.Clear();
			int i = 0;
			int count = data.Count;
			while (i < count)
			{
				this.auxPlayersInputData.Add(data[i].inputData);
				i++;
			}
		}

		public void PauseSimulation()
		{
			this.Pause();
            // 197
			this.RaiseEvent(SIMULATION_CODE, new byte[1], true, this.auxActivePlayersIds);
		}

		public void RunSimulation(bool firstRun)
		{
			this.Run();
			bool flag = !firstRun;
			if (flag)
			{
                // 197
				this.RaiseEvent(SIMULATION_CODE, new byte[]
				{
					1
				}, true, this.auxActivePlayersIds);
			}
		}

		public void EndSimulation()
		{
			this.End();
            // 197
			this.RaiseEvent(SIMULATION_CODE, new byte[]
			{
				3
			}, true, this.auxActivePlayersIds);
		}

		public void Destroy(IBody rigidBody)
		{
			rigidBody.TSDisabled = true;
			int key = this.GetSimulatedTick(this.GetSyncedDataTick()) + 1;
			bool flag = !this.bodiesToDestroy.ContainsKey(key);
			if (flag)
			{
				this.bodiesToDestroy[key] = new List<IBody>();
			}
			this.bodiesToDestroy[key].Add(rigidBody);
		}

		protected void CheckSafeRemotion(int refTick)
		{
			bool flag = this.bodiesToDestroy.ContainsKey(refTick);
			if (flag)
			{
				List<IBody> list = this.bodiesToDestroy[refTick];
				foreach (IBody current in list)
				{
					bool tSDisabled = current.TSDisabled;
					if (tSDisabled)
					{
						this.physicsManager.RemoveBody(current);
					}
				}
				this.bodiesToDestroy.Remove(refTick);
			}
			bool flag2 = this.playersDisconnect.ContainsKey(refTick);
			if (flag2)
			{
				this.playersDisconnect.Remove(refTick);
			}
		}

		private void DropLagPlayers()
		{
			List<TSPlayer> list = new List<TSPlayer>();
			int refTick = this.GetRefTick(this.GetSyncedDataTick());
			bool flag = refTick >= 0;
			if (flag)
			{
				int i = 0;
				int count = this.activePlayers.Count;
				while (i < count)
				{
					TSPlayer tSPlayer = this.activePlayers[i];
					bool flag2 = !tSPlayer.IsDataReady(refTick);
					if (flag2)
					{
						tSPlayer.dropCount++;
						list.Add(tSPlayer);
					}
					i++;
				}
			}
			int j = 0;
			int count2 = list.Count;
			while (j < count2)
			{
				TSPlayer p = list[j];
				this.CheckDrop(p);
				bool sendDataForDrop = list[j].GetSendDataForDrop(this.localPlayer.ID, this._syncedDataCacheDrop);
				if (sendDataForDrop)
				{
                    // 199
					this.communicator.OpRaiseEvent(SEND_CODE, SyncedData.Encode(this._syncedDataCacheDrop), true, null);
					SyncedData.pool.GiveBack(this._syncedDataCacheDrop[0]);
				}
				j++;
			}
		}

        // 如果不是录像模式；获取本地玩家的输入操作，添加到本地玩家的当前帧同步数据;发送本地玩家操作给其他玩家
        private SyncedData UpdateData()
		{
			bool flag = this.replayMode == ReplayMode.LOAD_REPLAY;
			SyncedData result;
			if (flag)
			{
				result = null;
			}
			else
			{
                // 获取本地玩家的输入操作，添加到本地玩家的当前帧同步数据
				SyncedData @new = SyncedData.pool.GetNew();
				@new.Init(this.localPlayer.ID, this.ticks);
				this.GetLocalData(@new.inputData);
				this.localPlayer.AddData(@new);
				bool flag2 = this.communicator != null;
				if (flag2)
				{
                    // 发送本地玩家操作给其他玩家
					this.localPlayer.GetSendData(this.ticks, this._syncedDataCacheUpdateData);
                    // 199
					this.communicator.OpRaiseEvent(SEND_CODE, SyncedData.Encode(this._syncedDataCacheUpdateData), true, this.auxActivePlayersIds);
				}
				result = @new;
			}
			return result;
		}

		public InputDataBase GetInputData(int playerId)
		{
			return this.players[(byte)playerId].GetData(this.GetSyncedDataTick()).inputData;
		}

        // 发送校验码
		private void SendInfoChecksum(int tick)
		{
			bool flag = this.replayMode == ReplayMode.LOAD_REPLAY;
			if (!flag)
			{
				SyncedInfo syncedInfo = this.bufferSyncedInfo.Current();
				syncedInfo.playerId = this.localPlayer.ID;
				syncedInfo.tick = tick;
				syncedInfo.checksum = this.GetChecksumForSyncedInfo();
				this.bufferSyncedInfo.MoveNext();
                // 198
				this.RaiseEvent(CHECKSUM_CODE, SyncedInfo.Encode(syncedInfo));
			}
		}

		private void RaiseEvent(byte eventCode, object message)
		{
			this.RaiseEvent(eventCode, message, true, null);
		}

		private void RaiseEvent(byte eventCode, object message, bool reliable, int[] toPlayers)
		{
			bool flag = this.communicator != null;
			if (flag)
			{
				this.communicator.OpRaiseEvent(eventCode, message, reliable, toPlayers);
			}
		}

		private void OnEventDataReceived(byte eventCode, object content)
		{
            // 199
			bool flag = eventCode == SEND_CODE;
			if (flag)
			{
				byte[] data = content as byte[];
				List<SyncedData> list = SyncedData.Decode(data);
				bool flag2 = list.Count > 0;
				if (flag2)
				{
					TSPlayer tSPlayer = this.players[list[0].inputData.ownerID];
					bool flag3 = !tSPlayer.dropped;
					if (flag3)
					{
						this.OnSyncedDataReceived(tSPlayer, list);
						bool flag4 = list[0].dropPlayer && tSPlayer.ID != this.localPlayer.ID && !this.players[list[0].dropFromPlayerId].dropped;
						if (flag4)
						{
							tSPlayer.dropCount++;
						}
					}
					else
					{
						int i = 0;
						int count = list.Count;
						while (i < count)
						{
							SyncedData.pool.GiveBack(list[i]);
							i++;
						}
					}
					SyncedData.poolList.GiveBack(list);
				}
			}
			else
			{
                // 198
				bool flag5 = eventCode == CHECKSUM_CODE;
				if (flag5)
				{
					byte[] infoBytes = content as byte[];
					this.OnChecksumReceived(SyncedInfo.Decode(infoBytes));
				}
				else
				{
                    // 197
					bool flag6 = eventCode == SIMULATION_CODE;
					if (flag6)
					{
						byte[] array = content as byte[];
						bool flag7 = array.Length != 0;
						if (flag7)
						{
							bool flag8 = array[0] == 0;
							if (flag8)
							{
								this.Pause();
							}
							else
							{
								bool flag9 = array[0] == 1;
								if (flag9)
								{
									this.Run();
								}
								else
								{
									bool flag10 = array[0] == 3;
									if (flag10)
									{
										this.End();
									}
								}
							}
						}
					}
					else
					{
                        // 196
						bool flag11 = eventCode == SYNCED_GAME_START_CODE;
						if (flag11)
						{
							byte[] infoBytes2 = content as byte[];
							SyncedInfo syncedInfo = SyncedInfo.Decode(infoBytes2);
							this.players[syncedInfo.playerId].sentSyncedStart = true;
						}
					}
				}
			}
		}

		private void OnChecksumReceived(SyncedInfo syncedInfo)
		{
			bool dropped = this.players[syncedInfo.playerId].dropped;
			if (!dropped)
			{
				this.checksumOk = true;
				SyncedInfo[] buffer = this.bufferSyncedInfo.buffer;
				for (int i = 0; i < buffer.Length; i++)
				{
					SyncedInfo syncedInfo2 = buffer[i];
					bool flag = syncedInfo2.tick == syncedInfo.tick && syncedInfo2.checksum != syncedInfo.checksum;
					if (flag)
					{
						this.checksumOk = false;
						break;
					}
				}
			}
		}

		protected List<SyncedData> GetTickData(int tick)
		{
			this.auxPlayersSyncedData.Clear();
			int i = 0;
			int count = this.activePlayers.Count;
			while (i < count)
			{
				this.auxPlayersSyncedData.Add(this.activePlayers[i].GetData(tick));
				i++;
			}
			return this.auxPlayersSyncedData;
		}

		public void AddPlayer(byte playerId, string playerName, bool isLocal)
		{
			TSPlayer tSPlayer = new TSPlayer(playerId, playerName);
			this.players.Add(tSPlayer.ID, tSPlayer);
			this.activePlayers.Add(tSPlayer);
			if (isLocal)
			{
				this.localPlayer = tSPlayer;
				this.localPlayer.sentSyncedStart = true;
			}
			this.UpdateActivePlayers();
			bool flag = this.replayMode == ReplayMode.RECORD_REPLAY;
			if (flag)
			{
				this.replayRecord.AddPlayer(tSPlayer);
			}
		}

        // 检测玩家掉线
		private bool CheckDrop(TSPlayer p)
		{
			bool flag = p != this.localPlayer && !p.dropped && p.dropCount > 0;
			bool result;
			if (flag)
			{
                // 如果掉线次数 > 在线玩家次数 -1
				int num = this.activePlayers.Count - 1;
				bool flag2 = p.dropCount >= num;
				if (flag2)
				{
					this.compoundStats.globalStats.GetInfo("panic").count = 0L;
					p.dropped = true;
                    // 从在线玩家列表移除
					this.activePlayers.Remove(p);
					this.UpdateActivePlayers();
					Debug.Log("Player dropped (stopped sending input)");

                    // 添加到 该帧， 玩家掉线列表
					int key = this.GetSyncedDataTick() + 1;
					bool flag3 = !this.playersDisconnect.ContainsKey(key);
					if (flag3)
					{
						this.playersDisconnect[key] = new List<byte>();
					}
					this.playersDisconnect[key].Add(p.ID);
					result = true;
					return result;
				}
			}
			result = false;
			return result;
		}
	}
}
