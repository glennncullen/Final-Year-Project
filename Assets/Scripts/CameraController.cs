using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour {

	public GameObject focusCameraOn;
	private Vector3 offset;

	// Use this for initialization
	void Start () {
		offset = transform.position - focusCameraOn.transform.position;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		transform.position = focusCameraOn.transform.position + offset;
	}
}
