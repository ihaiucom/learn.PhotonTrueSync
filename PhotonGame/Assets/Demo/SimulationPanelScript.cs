using TrueSync;
using UnityEngine;

public class SimulationPanelScript : MonoBehaviour {
    
    public void Update() {
        gameObject.SetActive(!PhotonNetwork.connected || PhotonNetwork.isMasterClient);
    }

    public void BtnRun() {
        TrueSyncManager.RunSimulation();
    }

    public void BtnPause() {
        TrueSyncManager.PauseSimulation();
    }

    public void BtnEnd() {
        TrueSyncManager.EndSimulation();
    }

}