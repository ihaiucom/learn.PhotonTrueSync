using UnityEngine;
using TrueSync;

/**
* @brief Manages ball instantiation.
**/
public class PongSyncManager : TrueSyncBehaviour {

    /**
    * @brief Prefab of the ball.
    **/
    public GameObject ballPrefab;

    /**
    * @brief Initial setup when game is started.
    **/
    public override void OnSyncedStart() {
		CreateBall();
    }

    /**
    * @brief Instantiates the ball's prefab.
    **/
    void CreateBall() {
        TrueSyncManager.SyncedInstantiate(this.ballPrefab, TSVector.zero, TSQuaternion.identity);
    }

}