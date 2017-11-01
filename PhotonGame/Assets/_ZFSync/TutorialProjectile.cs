using System.Collections;
using System.Collections.Generic;
using TrueSync;
using UnityEngine;

public class TutorialProjectile : TrueSyncBehaviour
{

    public FP speed = 15;
    public TSVector direction;
    private FP destroyTime = 3;

    public override void OnSyncedUpdate()
    {
        if (destroyTime <= 0)
        {
            TrueSyncManager.SyncedDestroy(this.gameObject);
        }
        tsTransform.Translate(direction * speed * TrueSyncManager.DeltaTime);
        destroyTime -= TrueSyncManager.DeltaTime;
    }

    public void OnSyncedTriggerEnter(TSCollision other)
    {
        if (other.gameObject.tag == "Player")
        {
            TutorialPlayerMovement hitPlayer = other.gameObject.GetComponent<TutorialPlayerMovement>();
            if (hitPlayer.owner != owner)
            {
                TrueSyncManager.SyncedDestroy(this.gameObject);
                hitPlayer.Respawn();
            }
        }
    }
}
