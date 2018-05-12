using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightStopX : MonoBehaviour
{
	private LightsController _controller;
//	[HideInInspector] 
	public CrossLane CrossLane;
	public Front Front;
//	[HideInInspector] 
	public VehicleBehaviour VehicleAtLight;
	public CrossLane LeftTurn;
	public CrossLane RightTurn;
	public CrossLane StraightOn;

	private void Awake()
	{
		_controller = GetComponentInParent<LightsController>();
		CrossLane = GetComponentInChildren<CrossLane>();
		Front = GetComponentInChildren<Front>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() == null) return;
		VehicleAtLight = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(VehicleAtLight == null) return;
		_controller.NotifyX(VehicleAtLight);
		VehicleAtLight.LightStopXs.Add(this);
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() == null) return;
		VehicleAtLight = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(VehicleAtLight == null) return;
		if (VehicleAtLight.NextRoad == null && !VehicleAtLight.IsUnableToMove)
		{
			VehicleAtLight.SetNextRoad();
		}
		VehicleAtLight.BuildNextPath();
		_controller.CheckRemoveX(VehicleAtLight);
		VehicleAtLight.LightStopXs.Remove(this);
		VehicleAtLight = null;
	}

	public void RemoveVehicle(VehicleBehaviour vehicle)
	{
		if (!ReferenceEquals(vehicle, VehicleAtLight)) return;
		_controller.CheckRemoveX(VehicleAtLight);
		VehicleAtLight = null;
	}
}
