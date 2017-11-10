using Photon;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialMenu : PunBehaviour
{

	void Start ()
    {
        PhotonNetwork.ConnectUsingSettings("v1.0");
        PhotonNetwork.autoJoinLobby = true;
        PhotonNetwork.automaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        RoomOptions roomOptions = new RoomOptions() {  Plugins = new string[] { "ZFCustomPlugin" } };
        PhotonNetwork.JoinOrCreateRoom("room1", roomOptions, null);
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 100, 30), "players: " + PhotonNetwork.playerList.Length);

        if (PhotonNetwork.isMasterClient && GUI.Button(new Rect(10, 40, 100, 30), "start"))
        {
            PhotonNetwork.LoadLevel("TutorialGame");
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
}
