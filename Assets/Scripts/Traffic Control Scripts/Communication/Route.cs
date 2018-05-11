using System.Collections;
using System.Collections.Generic;
using Traffic_Control_Scripts.Communication;
using UnityEngine;

public class Route : MonoBehaviour
{

	private void OnMouseDown()
	{
		Dictionary<string, object> message = new Dictionary<string, object>();
		message.Add("start", "Downey Memorial Way E1");
		message.Add("end", "Gorgeous Grove E6");
		PubNubBehaviour.Instance.PublishMessage("fire-in-progress", message);
//		message.Clear();
//		message.Add("extinguished", true);
//		PubNubBehaviour.Instance.PublishMessage("fire-extinguished", message);
	}
}
