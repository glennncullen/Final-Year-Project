using UnityEngine;
using System.Collections;
using System;

// Just an extension of CollisionDestry Script
// Refer to CollisionDestroy.cs
// Add this to those objects which should always play sound on collision
public class defaultAudio : MonoBehaviour {

	int index;
	public AudioSource[] audioEffects;
	public void playAudio() {
		index = UnityEngine.Random.Range(0,audioEffects.Length); /* randomly pick a audio source */

		try {
			audioEffects [index].playOnAwake=false;
			audioEffects [index].Play();
			//Debug.Log (randomIndex);
		} catch(Exception e) {
			throw new Exception ("Please add Audio Source or refer to manual for ... " + e.ToString());
		}
	}

	// Called on collider enter
	void OnCollisionEnter() {
//		playAudio ();
	}
}
