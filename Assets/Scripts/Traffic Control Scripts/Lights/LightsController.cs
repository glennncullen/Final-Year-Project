using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightsController : MonoBehaviour
{

	// light state
	private bool[] _xLightsRedYellowGreen;
	private bool[] _zLightsRedYellowGreen;
	
	// waiting vehicles
	private List<VehicleBehaviour> _vehiclesOnX;
	private List<VehicleBehaviour> _vehiclesOnZ;
	
	// initialise
	private void Awake()
	{
		_xLightsRedYellowGreen = GetComponentInChildren<TrafficLightControl>().GetLightsX();
		_zLightsRedYellowGreen = GetComponentInChildren<TrafficLightControl>().GetLightsZ();
		_vehiclesOnX = new List<VehicleBehaviour>();
		_vehiclesOnZ = new List<VehicleBehaviour>();
	}
	
	
	// update
	private void FixedUpdate()
	{
		GetLightState();
		if (_xLightsRedYellowGreen[2])
		{
			MoveTrafficOnX();
		}
		else if (_xLightsRedYellowGreen[0] || _xLightsRedYellowGreen[1])
		{
			StopTrafficOnX();
		}

		if (_zLightsRedYellowGreen[2])
		{
			MoveTrafficOnZ();
		}
		else if (_zLightsRedYellowGreen[0] || _zLightsRedYellowGreen[1])
		{
			StopTrafficOnZ();
		}
	}


	// update light state
	private void GetLightState()
	{
		_xLightsRedYellowGreen = GetComponentInChildren<TrafficLightControl>().GetLightsX();
		_zLightsRedYellowGreen = GetComponentInChildren<TrafficLightControl>().GetLightsZ();
	}
	
	
	// stop traffic on X but allow right crossing cars to pass
	// if they have been waiting
	private void StopTrafficOnX()
	{
		if (GetComponentInChildren<TrafficLightControl>().GetAllRed() && GetComponentInChildren<TrafficLightControl>().PreviousLightGreenX)
		{
			for (int i = _vehiclesOnX.Count - 1; i >= 0; i--)
			{
				VehicleBehaviour vehicle = _vehiclesOnX[i];
				if (!vehicle._rightCross) continue;
				vehicle.Continue();
				_vehiclesOnX.Remove(vehicle);
			}
		}
		foreach (VehicleBehaviour vehicle in _vehiclesOnX)
		{
			vehicle.Stop();
		}
	}
	
	
	// move traffic on X if the coast is clear
	private void MoveTrafficOnX()
	{
		int vehiclesTurningRight = 0;
		for(int i = _vehiclesOnX.Count-1; i >= 0; i--)
		{
			VehicleBehaviour vehicle = _vehiclesOnX[i];
			if (vehicle.NextRoad == null && !vehicle._isUnableToMove)
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
			if (vehicle._isUnableToMove)
			{
				vehicle.Stop();
			}
			if (!vehicle._rightCross)
			{
				foreach (LightStopX lightStop in GetComponentsInChildren<LightStopX>())
				{
					if (!ReferenceEquals(lightStop.VehicleAtLight, vehicle)) continue;
					if ((vehicle._isGoingStraightAtCross && !lightStop.StraightOn.TrafficInLane)
					    || (vehicle._leftCross && !lightStop.LeftTurn.TrafficInLane)
					    || (vehicle._leftJunctionJoin && !lightStop.LeftTurn.TrafficInLane)
					    || (vehicle._rightJunctionJoin && !lightStop.RightTurn.TrafficInLane)
					)
					{
						vehicle.Continue();
						_vehiclesOnX.Remove(vehicle);
					}
					else
					{
						vehicle.Stop();
					}
				}
			}
			else
			{
				foreach (LightStopX lightStop in GetComponentsInChildren<LightStopX>())
				{
					if (ReferenceEquals(lightStop.VehicleAtLight, vehicle) && !lightStop.CrossLane.TrafficInLane
					    && !lightStop.RightTurn.TrafficInLane)
					{
						vehicle.Continue();
						_vehiclesOnX.Remove(vehicle);
						break;
					}
					if (ReferenceEquals(lightStop.VehicleAtLight, vehicle) && lightStop.CrossLane.TrafficInLane 
					   												   && !lightStop.RightTurn.TrafficInLane)
					{
						vehiclesTurningRight++;
						vehicle.Stop();
					}
					else
					{
						vehicle.Stop();
					}
				}
			}
		}

		if (vehiclesTurningRight != 2) return;
		for(int i = _vehiclesOnX.Count-1; i >= 0; i--)
		{
			VehicleBehaviour vehicle = _vehiclesOnX[i];
			vehicle.Continue();
			_vehiclesOnX.Remove(vehicle);
		}
	}
	
	
	// stop traffic on Z but allow right crossing cars to pass
	// if they have been waiting
	private void StopTrafficOnZ()
	{
		if (GetComponentInChildren<TrafficLightControl>().GetAllRed() && GetComponentInChildren<TrafficLightControl>().PreviousLightGreenZ)
		{
			for (int i = _vehiclesOnZ.Count - 1; i >= 0; i--)
			{
				VehicleBehaviour vehicle = _vehiclesOnZ[i];
				if (!vehicle._rightCross && !vehicle._rightJunctionCrossing) continue;
				vehicle.Continue();
				_vehiclesOnZ.Remove(vehicle);
			}
		}
		foreach (VehicleBehaviour vehicle in _vehiclesOnZ)
		{
			vehicle.Stop();
		}
	}
	
	
	// move traffic on Z if the coast is clear
	private void MoveTrafficOnZ()
	{
		int vehiclesTurningRight = 0;
		for(int i = _vehiclesOnZ.Count-1; i >= 0; i--)
		{
			VehicleBehaviour vehicle = _vehiclesOnZ[i];
			if (vehicle.NextRoad == null && !vehicle._isUnableToMove)
			{
				vehicle.SetNextRoad();
			}
			if (vehicle.NextRoad != null)
			{
				if (vehicle.NextRoad.GetComponent<WaypointPath>().GetCongestion() >
				    vehicle.NextRoad.GetComponent<WaypointPath>().CongestionThreshold)
				{
					print(vehicle.gameObject.name + " setting another road on " + gameObject.name);
					vehicle.SetNextRoad();
				}
			}
			if (vehicle._isUnableToMove)
			{
				vehicle.Stop();
			}
			else if (vehicle._isGoingStraightAtCross || vehicle._leftCross
			    || vehicle._isGoingStraightAtJunction || vehicle._leftJunctionLeave)
			{
				foreach (LightStopZ lightStop in GetComponentsInChildren<LightStopZ>())
				{
					if (!ReferenceEquals(lightStop.VehicleAtLight, vehicle)) continue;
					if (((vehicle._isGoingStraightAtCross || vehicle._isGoingStraightAtJunction) && !lightStop.StraightOn.TrafficInLane)
					    || ((vehicle._leftCross || vehicle._leftJunctionLeave) && !lightStop.LeftTurn.TrafficInLane)
					)
					{
						vehicle.Continue();
						_vehiclesOnZ.Remove(vehicle);
					}
					else
					{
						vehicle.Stop();
					}
				}
			}
			else
			{
				foreach (LightStopZ lightStop in GetComponentsInChildren<LightStopZ>())
				{
					if (lightStop.CrossLane == null) continue;
					if (ReferenceEquals(lightStop.VehicleAtLight, vehicle) && !lightStop.CrossLane.TrafficInLane
					    											   && !lightStop.RightTurn.TrafficInLane)
					{
						vehicle.Continue();
						_vehiclesOnZ.Remove(vehicle);
						break;
					}

					if (ReferenceEquals(lightStop.VehicleAtLight, vehicle) && lightStop.CrossLane.TrafficInLane 
					                                                   && !lightStop.RightTurn.TrafficInLane)
					{
						vehiclesTurningRight++;
						vehicle.Stop();
					}
					else
					{
						vehicle.Stop();
					}
				}
			}
		}

		if (vehiclesTurningRight != 2) return;
		for(int i = _vehiclesOnZ.Count-1; i >= 0; i--)
		{
			VehicleBehaviour vehicle = _vehiclesOnZ[i];
			vehicle.Continue();
			_vehiclesOnZ.Remove(vehicle);
		}
	}
	

	
	// receive message from X axis collider
	public void NotifyX(VehicleBehaviour vehicle)
	{
		if (_vehiclesOnX.Contains(vehicle)) return;
		if (!vehicle._rightCross && !vehicle._rightJunctionCrossing)
		{
			_vehiclesOnX.Add(vehicle);
		}
		else
		{
			_vehiclesOnX.Insert(_vehiclesOnX.Count, vehicle);
		}
		
	}
	
	// receive message from z
	public void NotifyZ(VehicleBehaviour vehicle)
	{
		if (_vehiclesOnZ.Contains(vehicle)) return;
		if (!vehicle._rightCross && !vehicle._rightJunctionCrossing)
		{
			_vehiclesOnZ.Add(vehicle);
		}
		else
		{
			_vehiclesOnZ.Insert(_vehiclesOnZ.Count, vehicle);
		}
	}
	
	
	// make sure that vehicle has been removed from list
	public void CheckRemoveX(VehicleBehaviour vehicle)
	{
		if (!_vehiclesOnX.Contains(vehicle)) return;
		print(vehicle.gameObject.name + " not removed from X list in " + gameObject.name);
		_vehiclesOnX.Remove(vehicle);
	}
	
	
	// make sure that vehicle has been removed from list
	public void CheckRemoveZ(VehicleBehaviour vehicle)
	{
		if (!_vehiclesOnZ.Contains(vehicle)) return;
		print(vehicle.gameObject.name + " not removed from Z list in " + gameObject.name);
		_vehiclesOnX.Remove(vehicle);
	}
		
}
