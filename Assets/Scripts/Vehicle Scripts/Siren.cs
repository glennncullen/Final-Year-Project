using System.Collections;
using System.Collections.Generic;
using Traffic_Control_Scripts.Communication;
using UnityEngine;

public class Siren : MonoBehaviour {

	private void OnTriggerEnter(Collider other)
	{
		if (!Handler.IsSomethingOnFire) return;
		if (other.gameObject.GetComponent<CarFrontCollider>() == null) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		if (Handler.Path.Contains(vehicle._currentRoad.GetComponent<WaypointPath>())) return;
		if(vehicle._isBraking) return;
		if (vehicle.IsGoingStraightAtJunction || vehicle.RightJunctionCrossing ||
		    vehicle.RightJunctionJoin || vehicle.LeftJunctionJoin ||
		    vehicle.LeftJunctionLeave || vehicle.LeftCross || vehicle.RightCross ||
		    vehicle.IsGoingStraightAtCross) return;
		vehicle.EmergencyBrake = true;
		vehicle._brakeTorqueConstant = vehicle.MaxBrakeTorque;
		foreach (JunctionLane lane in vehicle.JunctionLanes)
		{
			lane.TrafficInLane = false;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!Handler.IsSomethingOnFire) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		if (Handler.Path.Contains(vehicle._currentRoad.GetComponent<WaypointPath>())) return;
		if(vehicle._isBraking) return;
		vehicle.EmergencyBrake = false;
		vehicle._brakeTorqueConstant = 0;
		foreach (JunctionLane lane in vehicle.JunctionLanes)
		{
			lane.TrafficInLane = true;
		}
	}
}
