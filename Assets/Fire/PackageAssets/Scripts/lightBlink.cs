using UnityEngine;
using System.Collections;
public class lightBlink : MonoBehaviour {
	int i;
	public float blinkTime; // Time between on and off transition of light or frequency of blinking
	public GameObject[] blinkLight; // Array of light (gameobject) to blink
	public bool alternateBlink; // For alternate blink, means that 2 lights will blink alternatively.

	// Normal blink light function
	IEnumerator blink() {
		while (true) {
			for(i=0;i<blinkLight.Length;i++) 
				blinkLight[i].SetActive (true);
			yield return new WaitForSeconds (blinkTime);
			for(i=0;i<blinkLight.Length;i++)
				blinkLight[i].SetActive (false);
			yield return new WaitForSeconds (blinkTime);
		}
	}

	// Function for alternate blinking
	// Means one light will be on, the other will be off. Vice versa, and process will continue
	IEnumerator alterBlink() {
		while(true) {
			for(i=0;i<blinkLight.Length;i++){
				if(i%2==0)
					blinkLight[i].SetActive(true);
				else 
					blinkLight[i].SetActive(false);
		}
			yield return new WaitForSeconds (blinkTime);
			for(i=0;i<blinkLight.Length;i++){
				if(i%2==0) 
					blinkLight[i].SetActive(false);
				else 
					blinkLight[i].SetActive(true);
			}
			yield return new WaitForSeconds (blinkTime);
		}
	}

	// Start blinking
	void Start () {
		if (!alternateBlink)
			StartCoroutine (blink ());
		if (alternateBlink)
			StartCoroutine (alterBlink ());
	}
}
