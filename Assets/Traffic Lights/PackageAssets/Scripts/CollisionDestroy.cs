using UnityEngine;
using System.Collections;
using System;

// Class for special effects
[System.Serializable]
public class uniqueEffects {
	
	bool hasPlayed = false; // For playing effects only once
	public Transform effectTransform; // At which position(transform), the effects will be played?
	public GameObject playEffect; // Play an effect like particle system at that position(transform)
	public AudioSource playSound; // Play a sound for that effect

	// To check if effect has played
	public bool getPlayedStatus() {
		return hasPlayed;
	}

	// Indicate effect has been played and do not play it again and again
	public void setPlayedStatus() {
		hasPlayed = true;
	}
}

public class CollisionDestroy : MonoBehaviour {

	int i; // loop variable
	static bool playNow = false; // Indicate when to play
	Component [] streetComponent; // Array of all rigidbody components in game object
	int randomIndex; // Random index
	public int minVelocityToDestroy; // Destruction will happen at this minimum velocity
	public uniqueEffects[] effects; // Array of efects to be played. Drag particle system prefabs here
	public AudioSource[] defaultAudioToPlay; //Array of sounds to be played
	public GameObject[] destroyObjects; // Array of objects to be destroyed after destruction
	Collider triggerComponent; // Trigger attached to GameObject. Must be at the absolute parent object

	// Randomly play different sounds for fun :)
	public void playAudio() {
		randomIndex = UnityEngine.Random.Range(0,defaultAudioToPlay.Length); // Randomly pick a audio source

		// There must be at least one audio source or else error will occur
		try {
			defaultAudioToPlay [randomIndex].playOnAwake=false; // Do not play audio on awake
			defaultAudioToPlay [randomIndex].Play(); // Play that audio
			//Debug.Log (randomIndex);
		} catch(Exception e) {
			throw new Exception ("Please add Audio Source" + e.ToString());
		}
	}

	// Initially set all rigidbodies as kinematic
	void setStatic() {
		foreach(Rigidbody rigid in streetComponent) {
			rigid.isKinematic = true;
			rigid.detectCollisions = true;
		}
	}

	// Set all ridigbodies as non-kinematic and indicate to play effects
	void destroyIfCollision() {
		foreach(Rigidbody rigid in streetComponent) {
			rigid.isKinematic = false;
			rigid.detectCollisions = true;
		}
		playNow = false;
		triggerComponent = GetComponent<Collider> ();
		triggerComponent.enabled = false; //disable trigger component
	}

	// Grab all rigidbodies
	void Start ()
	{
		streetComponent = GetComponentsInChildren (typeof(Rigidbody));
		setStatic ();
	}


	// Called on Trigger enter
	// Trigger must be on absolute parent object
	void OnTriggerEnter(Collider other) {
		if(playNow)	
//		playAudio ();
		// Destruction will occur at a minimum velocity
		// It can be 0 if you want no minimum velocity
		if (other.attachedRigidbody.velocity.magnitude > minVelocityToDestroy) {
			destroyIfCollision ();
			for(i=0;i<effects.Length;i++) {
				if(effects[i].getPlayedStatus()==false) {
					effects [i].setPlayedStatus ();
					// Instantiate effect prefabs at their specified transforms
					Instantiate (effects[i].playEffect, effects[i].effectTransform.transform.position, effects[i].effectTransform.transform.rotation);
					effects[i].playSound.Play ();
				}
			}
			// Destroy specific objects after collision like switch off lights etc
			for(i=0;i<destroyObjects.Length;i++) {
				Destroy (destroyObjects[i]);
			}
		} 
	}
}



