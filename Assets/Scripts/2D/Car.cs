using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;

public class Car : MonoBehaviour {

	//	private Vector2 move;
	private Rigidbody2D _rigidbodyComponent;
	public float TurnRotation = 3f;
	public float MaxForwardSpeed = 35f;
	public float MaxReverseSpeed = 10f;
	private float _currentSpeed = 0f;

	private List<Transform> _nodes;
	private int _currentNode;
	public Transform Path;

	[Header("Sensors")] 
	public Transform CenterSensor;
	public Transform LeftSensor;
	public Transform RightSensor;
	public float SensorLength = 3.5f;
	public float RaycastOffset = 0.35f;
	public float SensorAngle = 25f;
	private bool _isAvoiding;
	private float _avoidMultiplier = 0;
	

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
		_isAvoiding = false;
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
		Drive ();
		CheckCurrentWaypoint();
	}
	
	

	private void CheckSensors()
	{
		Vector2 direction = transform.up;
		RaycastHit2D leftSensorHit = Physics2D.Raycast(LeftSensor.position, direction, SensorLength);
		RaycastHit2D leftAngledSensorHit = Physics2D.Raycast(LeftSensor.position, Quaternion.AngleAxis(SensorAngle, transform.position) * direction, SensorLength);
		RaycastHit2D centerSensorHit = Physics2D.Raycast(CenterSensor.position, direction, SensorLength);
		RaycastHit2D rightSensorHit = Physics2D.Raycast(RightSensor.position, direction, SensorLength);
		RaycastHit2D rightAngledSensorHit = Physics2D.Raycast(RightSensor.position, Quaternion.AngleAxis(-SensorAngle, transform.position) * direction, SensorLength);
		_isAvoiding = false;
		_avoidMultiplier = 0;
		
		if (leftSensorHit)
		{
			Debug.DrawLine(LeftSensor.position, leftSensorHit.point, Color.green);
//			_currentSpeed -= (leftSensorHit.distance / 10);
			_avoidMultiplier -= ((leftSensorHit.distance % SensorLength) / 10);
			_isAvoiding = true;
		}
		if (leftAngledSensorHit)
		{
			Debug.DrawLine(LeftSensor.position, leftAngledSensorHit.point, Color.yellow);
//			_currentSpeed -= (leftAngledSensorHit.distance / 10);
			_avoidMultiplier -= ((leftAngledSensorHit.distance % SensorLength) / 5);
			_isAvoiding = true;
		}	
		
		if (rightSensorHit)
		{
			Debug.DrawLine(RightSensor.position, rightSensorHit.point, Color.green);
//			_currentSpeed -= (rightSensorHit.distance / 10);
			_avoidMultiplier += ((rightSensorHit.distance % SensorLength) / 10);
			_isAvoiding = true;
		}
		if (rightAngledSensorHit)
		{
			Debug.DrawLine(RightSensor.position, rightAngledSensorHit.point, Color.yellow);
//			_currentSpeed -= (rightAngledSensorHit.distance / 10);
			_avoidMultiplier += ((rightAngledSensorHit.distance % SensorLength) / 5);
			_isAvoiding = true;
		}
		
		if (_avoidMultiplier == 0)
		{
			if (centerSensorHit)
			{
				Debug.DrawLine(CenterSensor.position, centerSensorHit.point, Color.green);
//				_isAvoiding = true;
////				_currentSpeed -= (centerSensorHit.distance / 10);
//				Vector3 relativeVector = transform.InverseTransformPoint (_nodes[_currentNode].position);
//				if (relativeVector.x > 1)
//				{
//					_avoidMultiplier -= (centerSensorHit.distance / 10);
//				}
//				else
//				{
//					_avoidMultiplier += (centerSensorHit.distance / 10);
//				}
			}
		}

	}


	private void CheckCurrentWaypoint()
	{
		Vector3 relativeVector = transform.InverseTransformPoint (_nodes[_currentNode].position);
		if(relativeVector.x < 1 && relativeVector.x > -1 && relativeVector.y < 1 && relativeVector.y > -1){
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
		if (_isAvoiding)
		{
			if (_avoidMultiplier > 0)
			{
				transform.Rotate(Vector3.forward * (TurnRotation + _avoidMultiplier));
			}
			else
			{
				transform.Rotate(Vector3.forward * (-TurnRotation + _avoidMultiplier));
			}
		}
		else
		{
			if (relativeVector.x > 1) // if waypoint to the right
			{
				transform.Rotate(Vector3.forward * -TurnRotation); 
			}

			if (relativeVector.x < -1) // if waypoint is to the left
			{
				transform.Rotate(Vector3.forward * TurnRotation); 
			}
		}

		if (_currentSpeed > MaxForwardSpeed)
		{
			_currentSpeed = MaxForwardSpeed;
		}
		else if (_currentSpeed < 1)
		{
			_currentSpeed = 1;
		}
		
		if(relativeVector.y > 0.5){ // waypoint is above
			_rigidbodyComponent.AddForce (transform.up * _currentSpeed);
		}
		if(relativeVector.y > -0.5){ // waypoint is below
			_rigidbodyComponent.AddForce (-transform.up * (_currentSpeed / 3));
		}

		_currentSpeed++;
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
