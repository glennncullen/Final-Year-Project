using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarFrontCollider : MonoBehaviour
{

	private VehicleBehaviour _parent;
	
	private void Awake()
	{
		_parent = GetComponentInParent<VehicleBehaviour>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.GetComponent<CarBackCollider>() == null) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		if (ReferenceEquals(vehicle, _parent)) return;
		_parent.Stop();
	}
	
	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.GetComponent<CarBackCollider>() == null) return;
		_parent.Continue();
	}
}
