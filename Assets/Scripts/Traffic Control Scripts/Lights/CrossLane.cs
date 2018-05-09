using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossLane : MonoBehaviour {

//	[HideInInspector]
	public bool TrafficInLane;
	private List<VehicleBehaviour> _vehiclesInLane;

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
	}

//	private void OnTriggerStay(Collider other)
//	{
//		if (other.gameObject.GetComponent<CarFrontCollider>() != null) return;
//		if (other.gameObject.GetComponent<CarBackCollider>() != null) return;
//		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
//		if(vehicle == null) return;
//		TrafficInLane = true;
//	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() != null) return;
		if (other.gameObject.GetComponent<CarBackCollider>() != null) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		if (!_vehiclesInLane.Contains(vehicle))
		{
			print(vehicle.gameObject.name);
			Debug.Break();
		}

		_vehiclesInLane.Remove(vehicle);
		if (_vehiclesInLane.Count <= 0)
		{
			TrafficInLane = false;
		}
		else
		{
			TrafficInLane = true;
		}
	}
	
}
