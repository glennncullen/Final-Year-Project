using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JunctionLane : MonoBehaviour
{

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
		TrafficInLane = true;
		_vehiclesInLane.Add(vehicle);
		vehicle.JunctionLanes.Add(this);
	}

	
	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() != null) return;
		if (other.gameObject.GetComponent<CarBackCollider>() != null) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		_vehiclesInLane.Remove(vehicle);
		TrafficInLane = _vehiclesInLane.Count > 0;
		vehicle.JunctionLanes.Remove(this);
	}

	public void RemoveVehicle(VehicleBehaviour vehicle)
    	{
    		if (!_vehiclesInLane.Contains(vehicle)) return;
    		_vehiclesInLane.Remove(vehicle);
		    TrafficInLane = _vehiclesInLane.Count > 0;
    	}
}
