using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;

public class Car : MonoBehaviour {

	//	private Vector2 move;
	private Rigidbody2D _rigidbodyComponent;
	public float TurnRotation = 3f;
	public Vector2 Speed = new Vector2(5, 5);

	private List<Transform> _nodes;
	private int _currentNode;
	public Transform Path;

	[Header("Sensors")] 
	public Transform SensorOriginPoint;
	public float SensorLength = 3.5f;
	public float RaycastOffset = 0.35f;
	public float SensorAngle = 25f;
	

	// Use this for initialization
	void Start () {
		_rigidbodyComponent = GetComponent<Rigidbody2D> ();
		Transform[] pathTransforms = Path.GetComponentsInChildren<Transform>();
		_nodes = new List<Transform>();
		foreach(Transform pathTransform in pathTransforms){
			if(pathTransform != Path.transform){
				_nodes.Add (pathTransform);
			}
		}

		_currentNode = 0;
	}
	
	// 	Update is called once per frame
//	private void Update () {
//		float inputX = Input.GetAxis ("Horizontal");
//		float inputY = Input.GetAxis ("Vertical");
//
//		move = new Vector2 (
//			inputX * speed.x,
//			inputY * speed.y
//		);
//	}


	private void FixedUpdate()
	{
		CheckSensors();
//		Drive ();
		CheckCurrentWaypoint();
	}
	
	

	private void CheckSensors()
	{
		Vector2 direction = transform.up;
		Vector2 leftSensor = new Vector2(SensorOriginPoint.position.x - RaycastOffset, SensorOriginPoint.position.y);
		Vector2 middleSensor = SensorOriginPoint.position;
		Vector2 rightSensor = new Vector2(SensorOriginPoint.position.x + RaycastOffset, SensorOriginPoint.position.y);
		
		// left sensor
		RaycastHit2D hit = Physics2D.Raycast(leftSensor, direction, SensorLength);
		if (hit)
		{
			Debug.DrawLine(leftSensor, hit.point, Color.green);
		}
		
		// left angled sensor
		hit = Physics2D.Raycast(leftSensor, Quaternion.AngleAxis(SensorAngle, SensorOriginPoint.position) * direction, SensorLength);
		if (hit)
		{
			Debug.DrawLine(leftSensor, hit.point, Color.green);
		}	
		
		// middle sensor
		hit = Physics2D.Raycast(middleSensor, direction, SensorLength);
		if (hit)
		{
			Debug.DrawLine(middleSensor, hit.point, Color.green);
		}
		
		// right sensor
		hit = Physics2D.Raycast(rightSensor, direction, SensorLength);
		if (hit)
		{
			Debug.DrawLine(rightSensor, hit.point, Color.green);
		}		
		
		// right angled sensor
		hit = Physics2D.Raycast(rightSensor, Quaternion.AngleAxis(-SensorAngle, SensorOriginPoint.position) * direction, SensorLength);
		if (hit)
		{
			Debug.DrawLine(rightSensor, hit.point, Color.green);
		}	
		
	}


	private void CheckCurrentWaypoint()
	{
		Vector3 relativeVector = transform.InverseTransformPoint (_nodes[_currentNode].position);
		if(relativeVector.x < 2 && relativeVector.x > -2 && relativeVector.y < 2 && relativeVector.y > -2){
			if (_currentNode != _nodes.Count - 1) {
				_currentNode++;
			} else {
				_currentNode = 0;
			}
		}
	}


	void Drive(){
		Vector3 relativeVector = transform.InverseTransformPoint (_nodes[_currentNode].position);
//		print (relativeVector.x + " || " + relativeVector.y);
		if(relativeVector.x > 0.5){ // waypoint to the right
			transform.Rotate (Vector3.forward * -TurnRotation);
		}
		if(relativeVector.x < -0.5){ // waypoint is to the left
			transform.Rotate (Vector3.forward * TurnRotation);
		}
		if(relativeVector.y > 0.5){ // waypoint is above
			_rigidbodyComponent.AddForce (transform.up * Speed.x);
		}
		if(relativeVector.y > -0.5){ // waypoint is below
			_rigidbodyComponent.AddForce (-transform.up * Speed.y);
		}
	}

	
	// CODE SNIPPETS
	
	// Manual Drive
//	void FixedUpdate(){
//		if(Input.GetKey(KeyCode.UpArrow)){
//			_rigidbodyComponent.AddForce (transform.up * Speed.x);
//		}
//		if(Input.GetKey(KeyCode.DownArrow)){
//			_rigidbodyComponent.AddForce (-transform.up * Speed.y);
//		}
//		if (Input.GetKey (KeyCode.RightArrow)) {
//			transform.Rotate (Vector3.forward * -TurnRotation);
//		}
//		if (Input.GetKey (KeyCode.LeftArrow)) {
//			transform.Rotate (Vector3.forward * TurnRotation);
//		}
//	}
	
}
