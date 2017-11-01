using Photon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestPUNMenu : PunBehaviour
{
    [Header("Input")]
    public InputField inputVersion;
    public InputField inputRoomName;
    public InputField inputIP;
    public InputField inputPort;
    public InputField inputAppId;
    public InputField inputLog;
    [Header("Toggle")]
    public Toggle toggleAutoJoinLobby;
    public Toggle toggleEnableLobbyStats;

    public string version
    {
        get
        {
            return inputVersion.text;
        }

        set
        {
            inputVersion.text = value;
        }
    }

    public string roomName
    {
        get
        {
            return inputRoomName.text;
        }

        set
        {
            inputRoomName.text = value;
        }
    }


    public string ip
    {
        get
        {
            return inputIP.text;
        }
    }


    public int port
    {
        get
        {
            return Convert.ToInt32(inputPort.text);
        }
    }


    public string appid
    {
        get
        {
            return inputAppId.text ;
        }
    }

    public bool AutoJoinLobby
    {
        get
        {
            return toggleAutoJoinLobby.isOn;
        }

        set
        {
            toggleAutoJoinLobby.isOn = value;
        }
    }

    public bool EnableLobbyStats
    {
        get
        {
            return toggleEnableLobbyStats.isOn;
        }

        set
        {
            toggleEnableLobbyStats.isOn = value;
        }
    }


    public void ClearLog()
    {
        inputLog.text = "";
    }

    public void Log(string msg)
    {
        inputLog.text += msg + "\n";
    }

    public void LogFormat(String format, params object[] args)
    {
        Log(string.Format(format, args));
    }


    // Use this for initialization
    void Start () {
        UpdateToggle();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void UpdateToggle()
    {
        AutoJoinLobby = PhotonNetwork.autoJoinLobby;
        EnableLobbyStats = PhotonNetwork.EnableLobbyStatistics;
    }

    #region Connect
    public void TestConnectUsingSettings()
    {
        PhotonNetwork.ConnectUsingSettings(version);
    }

    public void TestConnect()
    {
        PhotonNetwork.ConnectToMaster(ip, port, appid, version);
    }

    public override void OnConnectionFail(DisconnectCause cause)
    {
        base.OnConnectionFail(cause);
        LogFormat("[OnConnectionFail] cause={0}", cause);
    }

    public void OnFailedToConnectToPhoton()
    {
        LogFormat("[OnFailedToConnectToPhoton]");
    }

    // 如果Auto-Join Lobby未选中，OnConnectedToMaster将调用回调。否则，OnConnectedToMaster将被跳过，只会OnJoinedLobby被调用。
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        LogFormat("[OnConnectedToMaster]");

    }

    #endregion

    #region Lobby

    public void TestJoinLobby()
    {
        PhotonNetwork.JoinLobby();
    }

    public void TestLeaveLobby()
    {
        PhotonNetwork.LeaveLobby() ;
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        foreach (RoomInfo room in PhotonNetwork.GetRoomList())
        {
            GUILayout.Label(string.Format("{0} {1}/{2}", room.name, room.playerCount, room.maxPlayers));
        }
    }
    #endregion

    #region Room
    public void TestCreateRoom()
    {
        bool result = PhotonNetwork.CreateRoom(roomName);
        LogFormat("[TestCreateRoom result={0}]", result);
    }

    public void TestJoinRoom()
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public void TestJoinRandRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }


    public void TestLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnCreatedRoom()
    {
        base.OnCreatedRoom();
        LogFormat("[OnCreatedRoom]");
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        LogFormat("[OnJoinedRoom] roomName={0}", PhotonNetwork.room.Name);
    }

    public override void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        base.OnPhotonCreateRoomFailed(codeAndMsg);

        LogFormat("[OnPhotonCreateRoomFailed] codeAndMsg={0}", codeAndMsg);
    }


    #endregion





}
