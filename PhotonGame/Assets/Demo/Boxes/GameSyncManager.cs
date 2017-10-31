using UnityEngine;
using TrueSync;

/**
* @brief Manages boxes instantiation.
**/
public class GameSyncManager : TrueSyncBehaviour {

    /**
    * @brief Box's prefab.
    **/
    public GameObject boxPrefab;

    /**
    * @brief Number of boxes to be place in X axis.
    **/
    public int numberOfBoxesX;

    /**
    * @brief Number of boxes to be place in Z axis.
    **/
    public int numberOfBoxesZ;

    /**
    * @brief Initial setup when game is started.
    **/
    public override void OnSyncedStart() {
        CreateBoxes();
    }

    /**
    * @brief Instantiates and places all boxes required by {@link #numberOfBoxesX} and {@link #numberOfBoxesZ}.
    **/
    void CreateBoxes() {
        for (int i = 0; i < numberOfBoxesX; i++) {
            for (int j = 0; j < numberOfBoxesZ; j++) {
                GameObject box = TrueSyncManager.SyncedInstantiate(this.boxPrefab, TSVector.zero, TSQuaternion.identity);
                TSRigidBody body = box.GetComponent<TSRigidBody>();
                body.position = new TrueSync.TSVector(i * 2 - 5, 1, j * 2);
            }
        }
    }

    /**
    * @brief Logs a text when game is paused.
    **/
    public override void OnGamePaused() {
        Debug.Log("Game Paused");
    }

    /**
    * @brief Logs a text when game is unpaused.
    **/
    public override void OnGameUnPaused() {
        Debug.Log("Game UnPaused");
    }

    /**
    * @brief Logs a text when game is ended.
    **/
    public override void OnGameEnded() {
        Debug.Log("Game Ended");
    }

    /**
    * @brief When a player get disconnected all objects belonging to him are destroyed.
    **/
    public override void OnPlayerDisconnection(int playerId) {
        TrueSyncManager.RemovePlayer(playerId);
    }

}