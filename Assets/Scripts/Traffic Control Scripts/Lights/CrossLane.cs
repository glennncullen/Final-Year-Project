﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossLane : MonoBehaviour {

//	[HideInInspector]
	public bool TrafficInLane;

	private void Awake()
	{
		TrafficInLane = false;
	}


	private void OnTriggerStay(Collider other)
	{
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		TrafficInLane = true;
	}

	private void OnTriggerExit(Collider other)
	{
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		TrafficInLane = false;
	}
	
}