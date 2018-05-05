using UnityEngine;
using System.Collections;
using System;
using System.Runtime.CompilerServices;
using UnityEngine.Analytics;


// Light box class
[System.Serializable]
public class lightBox {
	public GameObject redLight; // Red light object
	public GameObject yellowLight; // Yellow light object
	public GameObject greenLight; // Green light object

}

public class TrafficLightControl : MonoBehaviour
{
	public TrafficLightControl()
	{
		PreviousLightGreen = false;
		PreviousLightRed = false;
	}

	public bool PreviousLightRed { get; set; }
	public bool PreviousLightGreen { get; set; }

	public lightBox[] zAxis; // Array of all lightbox facing Z axis and -Z axis
	public lightBox[] xAxis; // Array of all lightbox facing X axis and -X axis
	public lightBox RoadFacing; // the waypoint path that ends at the Z axis
	int i;
	public float XGreenTime; // time for green light on xaxis
	public float ZGreenTime; // time for green on zaxis
	public float transitionTime;// Time for yellow light to stay

	// Function to set lights on or off
	// You must carefully set the lights. 
	// Lights facing Z direction must be in Z direction, and those facing X, must be in X direction
	// Put them accordingly an xAxis and zAxis array
	void setLights(bool xRed,bool xYellow, bool xGreen, bool zRed, bool zYellow, bool zGreen) {
		for(i=0;i<xAxis.Length;i++) {
			xAxis [i].redLight.SetActive (xRed);
			xAxis [i].yellowLight.SetActive (xYellow);
			xAxis [i].greenLight.SetActive (xGreen);
		}
		for(i=0;i<zAxis.Length;i++) {
			zAxis [i].redLight.SetActive (zRed);
			zAxis [i].yellowLight.SetActive (zYellow);
			zAxis [i].greenLight.SetActive (zGreen);
		}
	}

	// get the state of traffic lights facing the path
	public bool[] GetTrafficLightsFacing()
	{
		return new[]{RoadFacing.redLight.activeSelf, RoadFacing.yellowLight.activeSelf, RoadFacing.greenLight.activeSelf};
	}

	// Green lights facing X direction will be on
	// Red lights facing Z direction will be on
	void allowXdirection() {
		setLights (false, false, true, true, false, false);
		
	}

	// Green lights facing Z direction will be on
	// Red lights facing X direction will be on
	void allowZdirection() {
		setLights (true, false, false, false, false, true);
		
	}

	// All direction yellow lights will be on
	void allowWaiting() {
		setLights (false, true, false, false, true, false);
		
	}

	// Light control logic
	// Allow X and Z direction for specified period of time and vice versa
	IEnumerator startLights() {
		while(true) {
			allowXdirection ();
			PreviousLightGreen = RoadFacing.greenLight.activeSelf;
			PreviousLightRed = RoadFacing.redLight.activeSelf;
			yield return new WaitForSeconds (XGreenTime);
			allowWaiting ();
			yield return new WaitForSeconds (transitionTime);
			allowZdirection ();
			PreviousLightGreen = RoadFacing.greenLight.activeSelf;
			PreviousLightRed = RoadFacing.redLight.activeSelf;
			yield return new WaitForSeconds (ZGreenTime);
			allowWaiting ();
			yield return new WaitForSeconds (transitionTime);

		}
	}

	// Start traffic lights
	void Start ()
	{
		StartCoroutine (startLights());	
	}
		


}
