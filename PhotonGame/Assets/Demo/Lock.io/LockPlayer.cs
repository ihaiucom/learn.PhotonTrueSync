using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TrueSync;
using TrueSync.Physics3D;

public class LockPlayer : TrueSyncBehaviour {

	public Transform graphics;
	public GameObject nickPrefab;

	private byte MOUSE_X = 0;
	private byte MOUSE_Y = 1;

	// Click position
	private TSVector destination;
	public TSVector movement;
	public FP speed = 0.15;

	private SphereShape ss;
	private Text nickLabel;

    [AddTracking]
	public FP currentScale = FP.One;
	public FP growSlow = 1.125f;
	public FP growFast = 1.25f;

	private FP originalRadius;
	private Vector3 originalGraphicsScale;

	private bool onFocus = false;

	void OnApplicationFocus(bool focusStatus) {
		onFocus = focusStatus;
	}

    public override void OnSyncedStartLocalPlayer() {
        Camera.main.GetComponent<TopDownCamera>().target = transform;
    }

    public override void OnSyncedStart() {
		nickLabel = (Instantiate (nickPrefab) as GameObject).GetComponent<Text>();
		nickLabel.transform.SetParent (GameObject.Find ("Canvas").transform, false);
        nickLabel.text = owner.Name;
	
		ss = (SphereShape) GetComponent<TSSphereCollider> ().Shape;

		tsRigidBody.position += new TSVector (owner.Id * 2, 0, 0);
		originalRadius = ss.Radius;
		originalGraphicsScale = graphics.localScale;
	}

	public override void OnSyncedInput () {
		// if not on focus, send current position
		if (!onFocus) {
			TrueSyncInput.SetInt (MOUSE_X, (int) transform.position.x);
			TrueSyncInput.SetInt (MOUSE_Y, (int) transform.position.z);
			return;
		}

		// Unity raycast from mouse screen-pos to world-pos ("floor" plane)
		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
		if (Physics.Raycast (ray, out hit)) {
			// add tick-input to local queue and send to peers
			TrueSyncInput.SetInt (MOUSE_X, (int) hit.point.x);
			TrueSyncInput.SetInt (MOUSE_Y, (int) hit.point.z);
		}
	}

	public override void OnSyncedUpdate () {
		// receive click pos from input queue
		destination.x = TrueSyncInput.GetInt (MOUSE_X);
		destination.z = TrueSyncInput.GetInt (MOUSE_Y);
		UpdateShapes ();
		Steer ();
	}

	private void UpdateShapes() {
		ss.Radius = originalRadius * currentScale;
		graphics.localScale = originalGraphicsScale * ((float) currentScale);
	}

	private void Steer() {
		movement = destination - tsRigidBody.position;
		if (movement.sqrMagnitude < FP.One)
			return;
		movement.Normalize ();
		movement *= TrueSyncManager.DeltaTime * speed / currentScale;
        tsRigidBody.position += movement;
	}

	public void OnSyncedCollisionEnter(TSCollision other) {
		if (other.gameObject.tag == "Food") {
			// grow slow and destroy food
			currentScale *= growSlow;
			// move food to a new place (deterministic randoms)
			TSRigidBody food = other.rigidbody;
			food.position = new TSVector (TSRandom.Range(-35,35),0,TSRandom.Range(-20,20));
		} else if (other.gameObject.tag == "Player") {
			LockPlayer enemy = other.gameObject.GetComponent<LockPlayer> ();
			if (ss.Radius > enemy.ss.Radius) {
				// grow fast and eat other
				currentScale *= growFast;
				enemy.tsRigidBody.position = new TSVector (TSRandom.Range(-35,35),0,TSRandom.Range(-20,20));
				enemy.currentScale = FP.One;
				//TrueSyncManager.SyncedDestroy(other);
			}
		}
	}

	void LateUpdate() {
		if (nickLabel != null) {
			nickLabel.transform.position = Camera.main.WorldToScreenPoint(transform.position);
		}
	}
}
