using System;
using System.Collections;
using System.Collections.Generic;
using Traffic_Control_Scripts.Communication;
using UnityEngine;

public class Route : MonoBehaviour
{

	private void OnMouseDown()
	{
		if(Handler.IsSomethingOnFire) return;
		String road = GameObject.Find("Firebrigade").GetComponent<VehicleBehaviour>()._currentRoad.gameObject.name;
		Dictionary<string, object> message = new Dictionary<string, object>();
		message.Add("start", road);
		message.Add("end", "New Eggs Drive S2");
		Handler.Instance.PublishMessage("fire-in-progress", message);
	}
}

