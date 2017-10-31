using UnityEngine;
using System.Collections;

public class TopDownCamera : MonoBehaviour {

	public Transform target;
	public Transform[] layers;

	void LateUpdate () {
		if (target == null)
			return;
		Vector3 pos = transform.position;
		pos = target.transform.position;
		pos.y = 10;
		Vector3 diff = pos - transform.position;
		transform.position = pos;
		int div = 2;
		foreach (Transform t in layers) {
			t.Translate (-diff/div, Space.World);
			div *= 2;
		}
	}

}
