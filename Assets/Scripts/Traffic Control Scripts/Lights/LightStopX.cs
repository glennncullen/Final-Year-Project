using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightStopX : MonoBehaviour
{
	private LightsController _controller;
//	[HideInInspector] 
	public CrossLane CrossLane;
//	[HideInInspector] 
	public VehicleBehaviour VehicleAtLight;

	private void Awake()
	{
		_controller = GetComponentInParent<LightsController>();
		CrossLane = GetComponentInChildren<CrossLane>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() == null) return;
		VehicleAtLight = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(VehicleAtLight == null) return;
		VehicleAtLight.SetNextRoad();
		_controller.NotifyX(VehicleAtLight);
	}


	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() == null) return;
		VehicleAtLight = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(VehicleAtLight == null) return;
		VehicleAtLight.BuildNextPath();
		VehicleAtLight = null;
	}
}
