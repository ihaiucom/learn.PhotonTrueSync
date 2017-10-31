using UnityEngine;
using TrueSync;

/**
* @brief Manages player's ball behavior.        
**/
public class SimpleControl : TrueSyncBehaviour {

    /**
    * @brief Key to set/get horizontal position from {@link TrueSyncInput}.
    **/
    private const byte INPUT_KEY_HORIZONTAL = 0;

    /**
    * @brief Key to set/get vertical position from {@link TrueSyncInput}.
    **/
    private const byte INPUT_KEY_VERTICAL = 1;

    /**
    * @brief Key to set/get jump state from {@link TrueSyncInput}.
    **/
    private const byte INPUT_KEY_CREATE = 2;

    /**
    * @brief It is true if the ball is not dynamically instantiated.
    **/
    public bool createdRuntime;

    /**
    * @brief Ball's prefab.
    **/
    public GameObject prefab;

    /**
    * @brief Represents the last jump state.
    **/
    [AddTracking]
    private bool lastCreateState = false;

    /**
    * @brief Initial setup when game is started.
    **/
    public override void OnSyncedStart () {
        // if is first player then changes ball's color to black
		if (owner != null && owner.Id == 1) {
			GetComponent<Renderer> ().material.color = Color.black;
		}

        if (!createdRuntime) {
            tsRigidBody.position = new TrueSync.TSVector(-3 + (owner.Id - 1) * 4, 1, 16);
        }
	}

    /**
    * @brief Sets player inputs.
    **/
    public override void OnSyncedInput() {
        if (createdRuntime) {
            return;
        }

        float hor = Input.GetAxis("Horizontal");
        float ver = Input.GetAxis("Vertical");
        bool space = Input.GetKey(KeyCode.Space);

        TrueSyncInput.SetInt(INPUT_KEY_HORIZONTAL, (int)(hor * 100));
        TrueSyncInput.SetInt(INPUT_KEY_VERTICAL, (int)(ver * 100));
        TrueSyncInput.SetBool(INPUT_KEY_CREATE, space);
    }

    /**
    * @brief Updates ball's movements and instantiates new ball objects when player press space.
    **/
    public override void OnSyncedUpdate() {
		FP hor = (FP) TrueSyncInput.GetInt(INPUT_KEY_HORIZONTAL) / 100;
		FP ver = (FP) TrueSyncInput.GetInt(INPUT_KEY_VERTICAL) / 100;
        bool currentCreateState = TrueSyncInput.GetBool(INPUT_KEY_CREATE);

        // Instantiates a new ball belonging to current player if the following criteria is true
        if (!lastCreateState && currentCreateState && !createdRuntime) {
            SimpleControl otherSP = TrueSyncManager.SyncedInstantiate(prefab, tsTransform.position, tsTransform.rotation).GetComponent<SimpleControl>();
            otherSP.createdRuntime = true;
            otherSP.owner = owner;

            lastCreateState = currentCreateState;

            return;
        }

        TSVector forceToApply = TSVector.zero;

        if (FP.Abs(hor) > FP.Zero) {
            forceToApply.x = hor / 3;
        }

		if (FP.Abs(ver) > FP.Zero) {
            forceToApply.z = ver / 3;
        }

        tsRigidBody.AddForce(forceToApply, ForceMode.Impulse);

        lastCreateState = currentCreateState;
    }

    /**
    * @brief Tints box's material with gray color when it collides with the ball.
    **/
    public void OnSyncedCollisionEnter(TSCollision other) {
		if (other.gameObject.name == "Box(Clone)") {
			other.gameObject.GetComponent<Renderer> ().material.color = Color.gray;
		}
	}

    /**
    * @brief Increases box's local scale by 1% while collision with a ball remains active.
    **/
    public void OnSyncedCollisionStay(TSCollision other) {
		if (other.gameObject.name == "Box(Clone)") {
			other.gameObject.transform.localScale *= 1.01f;
		}
	}

    /**
    * @brief Resets changes in box's properties when there is no more collision with the ball.
    **/
    public void OnSyncedCollisionExit(TSCollision other) {
		if (other.gameObject.name == "Box(Clone)") {
			other.gameObject.transform.localScale = Vector3.one;
			other.gameObject.GetComponent<Renderer> ().material.color = Color.blue;
		}
	}

}