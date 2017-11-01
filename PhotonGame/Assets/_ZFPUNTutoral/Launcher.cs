using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;

namespace Tutoral.PUN
{
    public class Launcher : ZFPunCallbacks
    {
        #region Public Variables
        public PhotonLogLevel logLevel = PhotonLogLevel.Full;
        public string gameVersion = "1";

        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        public byte MaxPlayersPerRoom = 4;

        #endregion


        #region MonoBehaviour CallBacks
        private void Awake()
        {
            PhotonNetwork.autoJoinLobby = false;
            PhotonNetwork.automaticallySyncScene = true;
            PhotonNetwork.logLevel = logLevel;
        }

        #endregion

        #region Public Methods
        public void Connect()
        {
            if (PhotonNetwork.connected)
            {
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                PhotonNetwork.ConnectUsingSettings(gameVersion);
            }
        }
        #endregion


        #region Callback Event Methods
        public override void OnConnectedToMaster()
        {
            base.OnConnectedToMaster();
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
        {
            base.OnPhotonRandomJoinFailed(codeAndMsg);
            PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = MaxPlayersPerRoom }, null);
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
        }
        #endregion
    }

}
