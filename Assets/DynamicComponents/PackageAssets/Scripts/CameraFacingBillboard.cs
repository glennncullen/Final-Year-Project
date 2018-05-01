using UnityEngine;
using System.Collections;

public class CameraFacingBillboard : MonoBehaviour {
	Camera mainCam;
	void Awake() {
		mainCam = Camera.main; //Grab the main camera. Camera must be tagged as MainCamera
	}

	//Look at camera
	void Update() {
		Vector3 v = mainCam.transform.position - transform.position;
		v.x = v.z = 0.0f;
		transform.LookAt (mainCam.transform.position - v);
	}
}