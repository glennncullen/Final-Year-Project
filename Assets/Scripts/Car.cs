using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour {

	//	private Vector2 move;
	private Rigidbody2D rigidbodyComponent;
	public int turnRotation = 3;
	public Vector2 speed = new Vector2(5, 5);

	private List<Transform> nodes;
	private int currentNode = 0;
	public Transform path;

	// Use this for initialization
	void Start () {
		rigidbodyComponent = GetComponent<Rigidbody2D> ();
		Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
		nodes = new List<Transform>();
		foreach(Transform pathTransform in pathTransforms){
			if(pathTransform != path.transform){
				nodes.Add (pathTransform);
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
//		float inputX = Input.GetAxis ("Horizontal");
//		float inputY = Input.GetAxis ("Vertical");
//
//		move = new Vector2 (
//			inputX * speed.x,
//			inputY * speed.y
//		);

	}

	void OnTriggerEnter2D(Collider2D otherCollider){
		BoxCollider2D boundary = otherCollider.GetComponent<BoxCollider2D> ();
		if(boundary != null){

		}
	}

	void FixedUpdate(){
		moveWaypoint ();
		if(Input.GetKey(KeyCode.UpArrow)){
			rigidbodyComponent.AddForce (transform.up * speed.x);
		}
		if(Input.GetKey(KeyCode.DownArrow)){
			rigidbodyComponent.AddForce (-transform.up * speed.y);
		}
		if (Input.GetKey (KeyCode.RightArrow)) {
			transform.Rotate (Vector3.forward * -turnRotation);
		}
		if (Input.GetKey (KeyCode.LeftArrow)) {
			transform.Rotate (Vector3.forward * turnRotation);
		}
	}



	void moveWaypoint(){
		Vector3 relativeVector = transform.InverseTransformPoint (nodes[currentNode].position);
//		print (relativeVector.x + " || " + relativeVector.y);
		if(relativeVector.x > 0.5){ // waypoint to the right
			transform.Rotate (Vector3.forward * -turnRotation);
		}
		if(relativeVector.x < -0.5){ // waypoint is to the left
			transform.Rotate (Vector3.forward * turnRotation);
		}
		if(relativeVector.y > 0.5){ // waypoint is above
			rigidbodyComponent.AddForce (transform.up * speed.x);
		}
		if(relativeVector.y > -0.5){ // waypoint is below
			rigidbodyComponent.AddForce (-transform.up * speed.y);
		}
		if(relativeVector.x < 2 && relativeVector.x > -2 && relativeVector.y < 2 && relativeVector.y > -2){
			if (currentNode != nodes.Count - 1) {
				currentNode++;
			} else {
				currentNode = 0;
			}
		}
	}

}
