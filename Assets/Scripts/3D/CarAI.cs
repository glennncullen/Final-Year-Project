using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class CarAI : MonoBehaviour {
	
	private Rigidbody _rigidbodyComponent;
	public float MaxSpeed = 100f;
	public float MaxTorque = 35f;
	public float MaxBrakeTorque = 150f;
	public Vector3 CenterOfMass;
	private float _currentSpeed;
	public bool isBraking = false;
	
	public float MaxSteerAngle = 45f;
	
	[Header("Wheel Colliders")]
	public WheelCollider WheelFrontLeft;
	public WheelCollider WheelFrontRight;
	public WheelCollider WheelBackLeft;
	public WheelCollider WheelBackRight;

	public Transform Road;
	private List<Transform> _nodes;
	private int _currentNode;
	
	[Header("Sensors")] 
	public float SensorLength = 5f;
	public float FrontSensorPosition = 0.7f;
	public float SideSensorPosition = 0.3f;
	public float SensorAngle = 30f;
	private bool _isAvoiding;
	private float _avoidMultiplier = 0;

	// Use this for initialization
	void Start () {
		_rigidbodyComponent = GetComponent<Rigidbody> ();
		_rigidbodyComponent.centerOfMass = CenterOfMass;
		Transform[] pathTransforms = Road.GetComponentsInChildren<Transform>();
		_nodes = new List<Transform>();
		foreach(Transform navPoint in pathTransforms){
			if(navPoint.GetComponent<ERNavPoint>() != null){
				_nodes.Add (navPoint);
			}
		}
		_isAvoiding = false;
		_currentNode = 0;
	}
	
	// Update is called once per frame
	private void FixedUpdate ()
	{
		CheckSensors();
		Steer();
		Move();
		CheckCurrentNodeDistance();
		CheckBraking();
	}

	// functionality to steer car in correct direction
	private void Steer()
	{
		Vector3 relativeVector = transform.InverseTransformPoint(_nodes[_currentNode].position);
		relativeVector /= relativeVector.magnitude;
		float angle = (relativeVector.x / relativeVector.magnitude) * MaxSteerAngle;
		WheelFrontLeft.steerAngle = angle;
		WheelFrontRight.steerAngle = angle;
	}
	
	// functionality for moving the car
	private void Move()
	{
		_currentSpeed = 2 * Mathf.PI * WheelFrontLeft.radius * WheelFrontLeft.rpm * 60 / 1000;
		if (_currentSpeed < MaxSpeed && !isBraking)
		{
			WheelFrontLeft.motorTorque = MaxTorque;
			WheelFrontRight.motorTorque = MaxTorque;
		}
		else
		{
			WheelFrontLeft.motorTorque = 0f;
			WheelFrontRight.motorTorque = 0f;
		}
	}
	
	
	// functionality to update nodes
	private void CheckCurrentNodeDistance()
	{
		if(Vector3.Distance(transform.position, _nodes[_currentNode].position) < 0.5f){
			if (_currentNode != _nodes.Count - 1) {
				_currentNode++;
			} else {
				_currentNode = 0;
			}
		}
	}
	
	
	// functionality for car to brake
	private void CheckBraking()
	{
		if (isBraking)
		{
			WheelBackLeft.brakeTorque = MaxBrakeTorque;
			WheelBackRight.brakeTorque = MaxBrakeTorque;
		}
		else
		{
			WheelBackLeft.brakeTorque = 0;
			WheelBackRight.brakeTorque = 0;
		}
	}
	
	
	// functionality to check sensors
	private void CheckSensors()
	{
		RaycastHit hit;
		Vector3 sensorOriginPosition = transform.position;
		sensorOriginPosition.y += 0.25f;
		
		// front center sensor
		sensorOriginPosition.z += FrontSensorPosition;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, SensorLength))
		{
			
		}
		Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
		
		// front right sensor
		sensorOriginPosition.x += SideSensorPosition;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, SensorLength))
		{
			
		}
		Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
		
		// front left sensor
		sensorOriginPosition.x -= SideSensorPosition * 2;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, SensorLength))
		{
			
		}
		Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
	}
}
