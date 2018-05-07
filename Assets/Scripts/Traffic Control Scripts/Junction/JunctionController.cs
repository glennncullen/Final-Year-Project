using System.Collections;
using System.Collections.Generic;
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
		foreach(VehicleBehaviour vehicle in _vehiclesTurning)
		{
			if(vehicle._leftJunctionLeave || vehicle._isGoingStraightAtJunction) continue;
			if (vehicle._rightJunctionCrossing)
			{
				if (_rightLane.TrafficInLane)
				{
					vehicle.Stop();
					continue;
				}
				else
				{
					vehicle.Continue();
					continue;
				}
			}

			if (vehicle._rightJunctionJoin)
			{
				if (_rightLane.TrafficInLane || _leftLane.TrafficInLane)
				{
					vehicle.Stop();
					continue;
				}
				else
				{
					vehicle.Continue();
					continue;
				}
			}

			if (vehicle._leftJunctionJoin)
			{
				if (_rightLane.TrafficInLane)
				{
					vehicle.Stop();
				}
				else
				{
					vehicle.Continue();
				}
			}
		}
	}
	
	
	// get messages from stops
	public void Notify(VehicleBehaviour vehicle, bool remove)
	{
		if (remove)
		{
			_vehiclesTurning.Remove(vehicle);
			return;
		}

		if (_vehiclesTurning.Contains(vehicle)) return;
		_vehiclesTurning.Add(vehicle);
	}
}
