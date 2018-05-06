using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	public GameObject FocusCameraOn;
	private Vector3 _offset;

	// Use this for initialization
	void Start () {
		_offset = transform.position - FocusCameraOn.transform.position;
	}
	
	
	void LateUpdate () {
//		transform.position = focusCameraOn.transform.position + offset;
		transform.LookAt(FocusCameraOn.transform.position);
	}
}
