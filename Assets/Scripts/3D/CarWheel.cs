using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarWheel : MonoBehaviour
{

	public WheelCollider TargetWheel;

	private Vector3 WheelPosition = new Vector3();
	private Quaternion WheelRotation = new Quaternion();
	
	// Update is called once per frame
	void Update () {
		TargetWheel.GetWorldPose(out WheelPosition, out WheelRotation);
		transform.position = WheelPosition;
		transform.rotation = WheelRotation;
	}
}
