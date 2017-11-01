using System.Collections;
using System.Collections.Generic;
using TrueSync;
using UnityEngine;

public class TutorialPlayerWeapon : TrueSyncBehaviour
{
    public GameObject projectilePrefab;

    public override void OnSyncedInput()
    {
        if (Input.GetButton("Fire1"))
            TrueSyncInput.SetByte(2, 1);
        else
            TrueSyncInput.SetByte(2, 0);
    }

    private FP cooldown = 0;

    public override void OnSyncedUpdate()
    {
        byte fire = TrueSyncInput.GetByte(2);
        if (fire == 1 && cooldown <= 0)
        {
            GameObject projectileObject = TrueSyncManager.SyncedInstantiate(projectilePrefab, tsTransform.position, TSQuaternion.identity);

            TutorialProjectile projectile = projectileObject.GetComponent<TutorialProjectile>();
            projectile.direction = tsTransform.forward;
            projectile.owner = owner;

            cooldown = 1;
        }
        cooldown -= TrueSyncManager.DeltaTime;
    }

}
