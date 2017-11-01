using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;

public class ZFPunCallbacks : MonoBehaviour, IPunCallbacks
{

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        Loger.LogFormat("OnPhotonSerializeView stream.isWriting={0}", stream.isWriting);
    }

    public virtual void OnConnectedToPhoton()
    {
        Loger.LogFormat("[OnConnectedToPhoton]");
    }

    public virtual void OnLeftRoom()
    {
        Loger.LogFormat("[OnLeftRoom]");
    }

    public virtual void OnMasterClientSwitched(PhotonPlayer newMasterClient)
    {
        Loger.LogFormat("[OnMasterClientSwitched]");
    }

    public virtual void OnPhotonCreateRoomFailed(object[] codeAndMsg)
    {
        Loger.LogFormat("[OnPhotonCreateRoomFailed]");
    }

    public virtual void OnPhotonJoinRoomFailed(object[] codeAndMsg)
    {
        Loger.LogFormat("[OnPhotonJoinRoomFailed]");
    }

    public virtual void OnCreatedRoom()
    {
        Loger.LogFormat("[OnCreatedRoom]");
    }

    public virtual void OnJoinedLobby()
    {
        Loger.LogFormat("[OnJoinedLobby]");
    }

    public virtual void OnLeftLobby()
    {
        Loger.LogFormat("[OnLeftLobby]");
    }

    public virtual void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        Loger.LogFormat("[OnFailedToConnectToPhoton]");
    }

    public virtual void OnConnectionFail(DisconnectCause cause)
    {
        Loger.LogFormat("[OnConnectionFail] cause", cause);
    }

    public virtual void OnDisconnectedFromPhoton()
    {
        Loger.LogFormat("[OnDisconnectedFromPhoton]");
    }

    public virtual void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Loger.LogFormat("[OnPhotonInstantiate]");
    }

    public virtual void OnReceivedRoomListUpdate()
    {
        Loger.LogFormat("[OnReceivedRoomListUpdate]");
    }

    public virtual void OnJoinedRoom()
    {
        Loger.LogFormat("[OnJoinedRoom]");
    }

    public virtual void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        Loger.LogFormat("[OnPhotonPlayerConnected]");
    }

    public virtual void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        Loger.LogFormat("[OnPhotonPlayerDisconnected]");
    }

    public virtual void OnPhotonRandomJoinFailed(object[] codeAndMsg)
    {
        Loger.LogFormat("[OnPhotonRandomJoinFailed]");
    }

    public virtual void OnConnectedToMaster()
    {
        Loger.LogFormat("[OnConnectedToMaster]");
    }

    public virtual void OnPhotonMaxCccuReached()
    {
        Loger.LogFormat("[OnPhotonMaxCccuReached]");
    }

    public virtual void OnPhotonCustomRoomPropertiesChanged(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        Loger.LogFormat("[OnPhotonCustomRoomPropertiesChanged]");
    }

    public virtual void OnPhotonPlayerPropertiesChanged(object[] playerAndUpdatedProps)
    {
        Loger.LogFormat("[OnPhotonPlayerPropertiesChanged]");
    }

    public virtual void OnUpdatedFriendList()
    {
        Loger.LogFormat("[OnUpdatedFriendList]");
    }

    public virtual void OnCustomAuthenticationFailed(string debugMessage)
    {
        Loger.LogFormat("[OnCustomAuthenticationFailed]");
    }

    public virtual void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        Loger.LogFormat("[OnCustomAuthenticationResponse]");
    }

    public virtual void OnWebRpcResponse(OperationResponse response)
    {
        Loger.LogFormat("[OnWebRpcResponse]");
    }

    public virtual void OnOwnershipRequest(object[] viewAndPlayer)
    {
        Loger.LogFormat("[OnOwnershipRequest]");
    }

    public virtual void OnLobbyStatisticsUpdate()
    {
        Loger.LogFormat("[OnLobbyStatisticsUpdate]");
    }

    public virtual void OnPhotonPlayerActivityChanged(PhotonPlayer otherPlayer)
    {
        Loger.LogFormat("[OnPhotonPlayerActivityChanged]");
    }

    public virtual void OnOwnershipTransfered(object[] viewAndPlayers)
    {
        Loger.LogFormat("[OnOwnershipTransfered]");
    }

}
