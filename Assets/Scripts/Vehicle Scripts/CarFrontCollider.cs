using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarFrontCollider : MonoBehaviour
{

	private CarAI _parent;
	
	private void Awake()
	{
		_parent = GetComponentInParent<CarAI>();
	}

	private void OnTriggerEnter(Collider other)
	{
		CarAI car = other.gameObject.GetComponentInParent<CarAI>();
		if(car == null) return;
		print("This:\t" + gameObject.name + "\tColliding with:\t" + GetComponentInParent<CarAI>().gameObject.name);
		_parent.SlowDown();
	}

	private void OnTriggerExit(Collider other)
	{
		_parent.SpeedUp();
	}
}
