using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarBackCollider : MonoBehaviour {


    private void OnTriggerEnter(Collider other)
    {
//        print("This:\t" + gameObject.name + "\tColliding with:\t" + GetComponentInParent<CarAI>().gameObject.name);
    }
}
