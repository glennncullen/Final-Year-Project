using System.Collections;
using System.Collections.Generic;
using Traffic_Control_Scripts.Communication;
using UnityEngine;

public class JunctionController : MonoBehaviour
{

	// waiting vehicles
	private List<VehicleBehaviour> _vehiclesTurning;
	private JunctionLane _rightLane;
	private JunctionLane _leftLane;

	// initialise
	private void Awake()
	{
		_vehiclesTurning = new List<VehicleBehaviour>();
		JunctionLane[] lanes = GetComponentsInChildren<JunctionLane>();
		foreach (JunctionLane lane in lanes)
		{
			if (lane.gameObject.name.Equals("Lane to Right"))
			{
				_rightLane = lane;
			}
			else
			{
				_leftLane = lane;
			}
		}
	}
	
	// update
	private void FixedUpdate()
	{
		MoveVehicles();
	}
	
	
	// move vehicles if able
	private void MoveVehicles()
	{
		for(int i = _vehiclesTurning.Count-1; i >= 0; i--)
		{
			VehicleBehaviour vehicle = _vehiclesTurning[i];
			if (Handler.IsSomethingOnFire && vehicle.CompareTag("firebrigade"))
			{
				vehicle.SetNextRoad();
				vehicle.Continue();
				_vehiclesTurning.Remove(vehicle);
				continue;
			}
			if (vehicle.NextRoad == null)
			{
				vehicle.SetNextRoad();
			}

			if (vehicle.NextRoad != null)
			{
				if (vehicle.NextRoad.GetComponent<WaypointPath>().GetCongestion() >
				    vehicle.NextRoad.GetComponent<WaypointPath>().CongestionThreshold)
				{
					vehicle.SetNextRoad();
				}
			}

			if (vehicle.IsUnableToMove)
			{
				vehicle.Stop();
			}
			else if (vehicle.LeftJunctionLeave || vehicle.IsGoingStraightAtJunction)
			{
				_vehiclesTurning.Remove(vehicle);
			}
			else if (vehicle.RightJunctionCrossing)
			{
				if (_rightLane.TrafficInLane)
				{
					vehicle.Stop();
				}
				else
				{
					vehicle.Continue();
					_vehiclesTurning.Remove(vehicle);
				}
			}
			else if (vehicle.RightJunctionJoin)
			{
				if (_rightLane.TrafficInLane || _leftLane.TrafficInLane)
				{
					vehicle.Stop();
				}
				else
				{
					vehicle.Continue();
					_vehiclesTurning.Remove(vehicle);
				}
			}
			else if (vehicle.LeftJunctionJoin)
			{
				if (_rightLane.TrafficInLane)
				{
					vehicle.Stop();
				}
				else
				{
					vehicle.Continue();
					_vehiclesTurning.Remove(vehicle);
				}
			}
		}
	}
	
	
	// get messages from stops
	public void Notify(VehicleBehaviour vehicle)
	{
		if (_vehiclesTurning.Contains(vehicle)) return;
		_vehiclesTurning.Add(vehicle);
	}
	
	
	// make sure that vehicle has been removed from list
	public void CheckRemove(VehicleBehaviour vehicle)
	{
		if (!_vehiclesTurning.Contains(vehicle)) return;
		print(vehicle.gameObject.name + " not removed from JUNCTION list on " + gameObject.name);
		_vehiclesTurning.Remove(vehicle);
	}
	
}
