using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{

	public Transform Road;

	[HideInInspector] public bool IsOccupied;

	private void OnTriggerStay(Collider other)
	{
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		IsOccupied = true;
	}

	private void OnTriggerExit(Collider other)
	{
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		IsOccupied = false;
	}
}
