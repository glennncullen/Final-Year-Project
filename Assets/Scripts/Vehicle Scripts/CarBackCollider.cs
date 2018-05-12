using System.Collections;
using System.Collections.Generic;
using Traffic_Control_Scripts.Communication;
using UnityEngine;

public class CarBackCollider : MonoBehaviour {

    
    private VehicleBehaviour _parent;
    private float RegularSpeed;
	
    private void Awake()
    {
        _parent = GetComponentInParent<VehicleBehaviour>();
        RegularSpeed = _parent.MaxSpeed;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Handler.IsSomethingOnFire) return;
        if (other.gameObject.GetComponent<CarFrontCollider>() == null) return;
        VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
        if(vehicle == null) return;
        if (ReferenceEquals(vehicle, _parent)) return;
        if (!vehicle.CompareTag("firebrigade")) return;
        if (_parent.EmergencyBrake)
        {
            _parent.EmergencyBrake = false;
            _parent._brakeTorqueConstant = 0f;
        }
        _parent.MaxSpeed = vehicle._speedConstant;
    }
	
    private void OnTriggerExit(Collider other)
    {
        if (!Handler.IsSomethingOnFire) return;
        if (other.gameObject.GetComponent<CarBackCollider>() == null) return;
        VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
        if(vehicle == null) return;
        if (vehicle.CompareTag("firebrigade"))
        {
            _parent._speedConstant = RegularSpeed;
        }
    }
    
}
