using UnityEngine;
using System.Collections;

// To control light effects like volumetric lights, lens flares, halo, spotlights etc.
// If you do not want light effects like in day scenes, just uncheck light status

public class lightsControl : MonoBehaviour {

	public GameObject volumetricLight; // which volmetric light gameobject?
	public GameObject lightSource; // which light source gameobject?
	public GameObject lensFlare; // which lens flare or halo gameobject?
	
	public bool lightStatus=true; // Above objects on or off. 
	// Uncheck this if you do not want light effects like valumetric lights, lens flares or spotlights

	//------------------------------------------------------------------------------------------------

	// Function to switch on or off light effects. Means set their status
	// This function is Public 
	// It is public so that you can use it in your own scripts
	// Like a script to control time of day and accordingly set light effect status
	public void setLights(bool lights) {
		if(!lights) {
			volumetricLight.SetActive (false);
			lightSource.SetActive (false);
			lensFlare.SetActive (false);
		}
		if(lights) {
			volumetricLight.SetActive (true);
			lightSource.SetActive (true);
			lensFlare.SetActive (true);
		}
	}
	//------------------------------------------------------------------------------------------------

	// Set light effect status
	void Start()
	{
		print("Light Control:\t" + gameObject.name);
		setLights (lightStatus);
	}
}
