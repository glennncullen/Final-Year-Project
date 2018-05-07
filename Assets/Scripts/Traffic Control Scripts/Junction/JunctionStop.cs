﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JunctionStop : MonoBehaviour
{
	private JunctionController _controller;

	private void Awake()
	{
		_controller = GetComponentInParent<JunctionController>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() == null) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		vehicle.SetNextRoad();
		_controller.Notify(vehicle, false);
	}


	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() == null) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		vehicle.BuildNextPath();
		_controller.Notify(vehicle, true);
	}
}