using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Tutoral.PUN
{
    public class Launcher : ZFPunCallbacks
    {
        #region Public Variables
        public PhotonLogLevel logLevel = PhotonLogLevel.Full;
        public string gameVersion = "1";

        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        public byte MaxPlayersPerRoom = 4;

        [Tooltip("The Ui Panel to let the user enter name, connect and play")]
        public GameObject controlPanel;
        [Tooltip("The UI Label to inform the user that the connection is in progress")]
        public GameObject progressLabel;

        public Text progressText;

        public void SetState(string txt)
        {
            progressText.text = txt;
        }

        #endregion

        #region private Variables
        /// <summary>
        /// Keep track of the current process. Since connection is asynchronous and is based on several callbacks from Photon, 
        /// we need to keep track of this to properly adjust the behavior when we receive call back by Photon.
        /// Typically this is used for the OnConnectedToMaster() callback.
        /// </summary>
        private bool isConnecting;
        #endregion


        #region MonoBehaviour CallBacks
        private void Awake()
        {
            PhotonNetwork.autoJoinLobby = false;
            PhotonNetwork.automaticallySyncScene = true;
            PhotonNetwork.logLevel = logLevel;
        }

        private void Start()
        {
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
        }

        #endregion

        #region Public Methods
        public void Connect()
        {
            progressLabel.SetActive(true);
            controlPanel.SetActive(false);
            isConnecting = true;

            if (PhotonNetwork.connected)
            {
                SetState("已经连接成功,正在加入房间...");
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                SetState("正在连接...");
                PhotonNetwork.ConnectUsingSettings(gameVersion);
            }
        }
        #endregion


        #region Callback Event Methods

        public override void OnDisconnectedFromPhoton()
        {
            base.OnDisconnectedFromPhoton();
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
            SetState("断开连接");
        }

        public override void OnConnectedToMaster()
        {
            SetState("连接成功");
            base.OnConnectedToMaster();
            if(isConnecting)
            {
                PhotonNetwork.JoinRandomRoom();
            }
        }

        public override void OnPhotonRandomJoinFailed(object[] codeAndMsg)
        {
            SetState("加入房间失败");
            base.OnPhotonRandomJoinFailed(codeAndMsg);
            SetState("正在创建房间");
            PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = MaxPlayersPerRoom, Plugins = new string[] { "ZFCustomPlugin" } }, null);
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            SetState("加入房间完成");

            // #Critical: We only load if we are the first player, else we rely on  PhotonNetwork.automaticallySyncScene to sync our instance scene.
            if (PhotonNetwork.room.PlayerCount == 1)
            {
                Debug.Log("We load the 'Room for 1' ");


                // #Critical
                // Load the Room Level. 
                PhotonNetwork.LoadLevel("Room for 1");
            }
        }
        #endregion

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.F2))
            {

                SceneManager.LoadScene(0);
            }
        }
    }

}
