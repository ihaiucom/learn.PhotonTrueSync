using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using System.Threading;
using UnityEngine;

public enum ChatOp : byte
{
    Msg = 1,
}

public enum ChatMsgParameterKey : byte
{
    Content = 1,
}

public class ZFChatClient : MonoBehaviour , IPhotonPeerListener
{
    private bool connected;
    PhotonPeer peer;


    public void CreateConnnect()
    {
        peer = new PhotonPeer(this, ConnectionProtocol.Tcp);
        DebugReturn(DebugLevel.INFO, "Connecting to server at 127.0.0.1:7571 using TCP");
        peer.Connect("127.0.0.1:7571", "ZFChatServer");
    }

    void Update()
    {
        if(peer != null)
        {
            peer.Service();
        }

    }

    private string inputText = "";
    private string outText = "";
    void OnGUI()
    {
        if(!connected)
        {
            if(GUI.Button(new Rect(10, 10, Screen.width - 20, Screen.height * 0.3f), "Connect"))
            {
                CreateConnnect();
            }
        }

        if (connected)
        {
            inputText = GUI.TextArea(new Rect(10, 10, Screen.width - Screen.width*0.7f - 20, Screen.height * 0.3f), inputText);
            if(GUI.Button(new Rect(Screen.width * 0.7f + 30, 10, Screen.width * 0.2f, Screen.height * 0.3f), "Send"))
            {
                // send to server
                var parameters = new Dictionary<byte, object> { { (byte)ChatMsgParameterKey.Content, inputText } };

                peer.OpCustom((byte)ChatOp.Msg, parameters, true);
            }
        }

        GUI.TextArea(new Rect(10, Screen.height - Screen.height * 0.5f - 10, Screen.width -20, Screen.height * 0.5f), outText);
    }

    #region IPhotonPeerListener

    public void DebugReturn(DebugLevel level, string message)
    {
        outText += string.Format("{0}: {1}\n", level, message);
    }

    public void OnEvent(EventData eventData)
    {
        DebugReturn(DebugLevel.INFO, eventData.ToStringFull());
        if (eventData.Code == 1)
        {
            DebugReturn(DebugLevel.INFO, string.Format("Chat Message: {0}", eventData.Parameters[1]));
        }
    }

    public void OnMessage(object messages)
    {
        throw new NotImplementedException();
    }

    public void OnOperationResponse(OperationResponse operationResponse)
    {
        DebugReturn(DebugLevel.INFO, operationResponse.ToStringFull());
    }

    public void OnStatusChanged(StatusCode statusCode)
    {
        if (statusCode == StatusCode.Connect)
        {
            connected = true;
        }
        switch (statusCode)
        {
            case StatusCode.Connect:
                DebugReturn(DebugLevel.INFO, "Connected");
                connected = true;
                break;
            case StatusCode.Disconnect:
                connected = false;
                DebugReturn(DebugLevel.ERROR, statusCode.ToString());
                break;
            default:
                DebugReturn(DebugLevel.ERROR, statusCode.ToString());
                break;
        }
    }

    #endregion
}