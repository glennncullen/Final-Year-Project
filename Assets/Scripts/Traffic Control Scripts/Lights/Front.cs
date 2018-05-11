using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Front : MonoBehaviour {

	//	[HideInInspector]
	public bool TrafficInLane;
	private List<VehicleBehaviour> _vehiclesInLane = new List<VehicleBehaviour>();

	private void Awake()
	{
		TrafficInLane = false;
	}


	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() != null) return;
		if (other.gameObject.GetComponent<CarBackCollider>() != null) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		if (!vehicle._rightCross && !vehicle._rightJunctionCrossing) return;
		TrafficInLane = true;
		_vehiclesInLane.Add(vehicle);
		vehicle.Fronts.Add(this);
	}

	
	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() != null) return;
		if (other.gameObject.GetComponent<CarBackCollider>() != null) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		_vehiclesInLane.Remove(vehicle);
		TrafficInLane = _vehiclesInLane.Count > 0;
		vehicle.Fronts.Remove(this);
	}
	
	public void RemoveVehicle(VehicleBehaviour vehicle)
	{
		if (!_vehiclesInLane.Contains(vehicle)) return;
		_vehiclesInLane.Remove(vehicle);
	}
}
