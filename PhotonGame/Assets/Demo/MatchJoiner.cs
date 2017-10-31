using UnityEngine;
using UnityEngine.UI;

// attached to each room on join list to do a join if the players hit the button
public class MatchJoiner : MonoBehaviour {

	public Text btnText;

	private string roomName;

	public void Join () {
		PhotonNetwork.JoinRoom(this.roomName);
	}

	public void UpdateRoom(RoomInfo room) {
		this.roomName = room.Name;
		btnText.text = this.roomName;
	}

}