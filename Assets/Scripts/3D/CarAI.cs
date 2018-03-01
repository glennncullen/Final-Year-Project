using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class CarAI : MonoBehaviour {
	
	private Rigidbody _rigidbodyComponent;
	public float MaxForwardSpeed = 35f;
	public float MaxReverseSpeed = 10f;
	private float _currentSpeed = 0f;
	
	public float MaxSteerAngle = 45f;
	public WheelCollider WheelFrontLeft;
	public WheelCollider WheelFrontRight;

	public Transform Road;
	private List<Transform> _nodes;
	private int _currentNode;
	
	[Header("Sensors")] 
	public Transform SensorOriginPosition;
	public float SensorLength = 3.5f;
	public float RaycastOffset = 0.35f;
	public float SensorAngle = 25f;
	private bool _isAvoiding;
	private float _avoidMultiplier = 0;

	// Use this for initialization
	void Start () {
		_rigidbodyComponent = GetComponent<Rigidbody> ();
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
		Steer();
		Move();
		CheckCurrentNodeDistance();
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
		WheelFrontLeft.motorTorque = 50f;
		WheelFrontRight.motorTorque = 50f;
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
	
}
