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
	
	
	
	// move traffic on X if the coast is clear
	private void MoveTrafficOnX()
	{
		int vehiclesTurningRight = 0;
		for(int i = _vehiclesOnX.Count-1; i >= 0; i--)
		{
			VehicleBehaviour vehicle = _vehiclesOnX[i];
			if (!vehicle._rightCross)
			{
				vehicle.Continue();
				_vehiclesOnX.Remove(vehicle);
			}
			else
			{
				foreach (LightStopX light in GetComponentsInChildren<LightStopX>())
				{
					if (ReferenceEquals(light.VehicleAtLight, vehicle) && !light.CrossLane.TrafficInLane)
					{
						vehicle.Continue();
						_vehiclesOnX.Remove(vehicle);
						break;
					}
					if (!ReferenceEquals(light.VehicleAtLight, vehicle) || !light.CrossLane.TrafficInLane) continue;
					vehicle.Stop();
					vehiclesTurningRight++;
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
				if (vehicle._rightCross || vehicle._rightJunctionCrossing)
				{
					vehicle.Continue();
					_vehiclesOnZ.Remove(vehicle);
				}
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
			if (vehicle._isGoingStraightAtCross || vehicle._leftCross
			    || vehicle._isGoingStraightAtJunction || vehicle._leftJunctionLeave)
			{
				vehicle.Continue();
				_vehiclesOnZ.Remove(vehicle);
			}
			else
			{
				foreach (LightStopZ light in GetComponentsInChildren<LightStopZ>())
				{
					if (ReferenceEquals(light.VehicleAtLight, vehicle) && !light.CrossLane.TrafficInLane)
					{
						vehicle.Continue();
						_vehiclesOnZ.Remove(vehicle);
						break;
					}

					if (!ReferenceEquals(light.VehicleAtLight, vehicle) || !light.CrossLane.TrafficInLane) continue;
					vehicle.Stop();
					vehiclesTurningRight++;
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
	
	
	// stop traffic on X but allow right crossing cars to pass
	// if they have been waiting
	private void StopTrafficOnX()
	{
		if (GetComponentInChildren<TrafficLightControl>().GetAllRed() && GetComponentInChildren<TrafficLightControl>().PreviousLightGreenX)
		{
			for (int i = _vehiclesOnX.Count - 1; i >= 0; i--)
			{
				VehicleBehaviour vehicle = _vehiclesOnX[i];
				if (vehicle._rightCross)
				{
					vehicle.Continue();
					_vehiclesOnX.Remove(vehicle);
				}
			}
		}
		foreach (VehicleBehaviour vehicle in _vehiclesOnX)
		{
			vehicle.Stop();
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
		
}
