using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Scripting.APIUpdating;
using Debug = UnityEngine.Debug;

public class VehicleBehaviour : MonoBehaviour {
	
	[Header("Vehicle Properties")]
	public Vector3 CenterOfMass = new Vector3(0f, -0.2f, 0f);
	public float MaxSpeed = 50f;
	public float MaxTorque = 1000f;
	public float MaxSteerAngle = 55f;
	public float MaxTurningSpeed = 40f;
	public float MaxBrakeTorque = 2000f;
	

	[Header("Wheel Colliders")]
	public WheelCollider WheelFrontLeft;
	public WheelCollider WheelFrontRight;
	public WheelCollider WheelBackLeft;
	public WheelCollider WheelBackRight;

	[Header("Route To Follow")]
	public Transform StartingRoad;
	public float DistanceFromWpToChange = 3f;
	
	[Header("Debug Properties")]
	public bool StopVehicle = false;

	// constants
	private float _currentSpeed;
	private float _speedConstant;
	private float _brakeTorqueConstant;
	
	// route control
	private List<Transform> _pathNodes;
	private int _currentPathNode;
	private Transform _currentRoad;
	private Transform _nextRoad;
	private Transform _previousRoad;
	
	// vehicle attributes
	private bool _isBraking;
	private float _targetSteerAngle;
	public float _smoothTurningSpeed = 5f;
	
	
	// private variables
	private Rigidbody _rigidbodyComponent;
	
//	[HideInInspector]
//	public bool _isChangingPath;
//	[HideInInspector]
//	public bool _isSafeToChangePath;
//	[HideInInspector]
	public bool _leftCross;
//	[HideInInspector]
	public bool _rightCross;
//	[HideInInspector]
	public bool _leftJunctionJoin;
//	[HideInInspector]
	public bool _rightJunctionJoin;
//	[HideInInspector]
	public bool _leftJunctionLeave;
//	[HideInInspector]
	public bool _rightJunctionCrossing;
//	[HideInInspector]
	public bool _isGoingStraightAtJunction;
//	[HideInInspector]
	public bool _isGoingStraightAtCross;
	
	
	//	[Header("Sensors")] 
//	public float FrontSensorLength = 7.5f;
//	public Vector3 FrontSensorPosition = new Vector3(0f, 1.5f, 3.7f);
//	public float SideSensorPosition = 1f;
//	public Vector3 SensorsToLookAhead = new Vector3(15f, 25f, 35f); // angle, angle, length
//	public Vector4 SensorToLookRight = new Vector4(80f, 30f, 70f, 20f); // angle, length, angle, length
//	public Vector4 SensorToLookLeft = new Vector4(-40f, 30f, -20f, 15f); // angle, length, angle, length
	
	
	
	

	// initialization
	private void Awake () {
		_rigidbodyComponent = GetComponent<Rigidbody> ();
		_rigidbodyComponent.centerOfMass = CenterOfMass;
		_currentRoad = StartingRoad;
		Transform[] pathTransforms = StartingRoad.GetComponentsInChildren<Transform>();
		_pathNodes = new List<Transform>();
		foreach(Transform navPoint in pathTransforms){
			if (StartingRoad.transform != navPoint)
			{
				_pathNodes.Add(navPoint);
			}
		}
		_speedConstant = MaxSpeed;
		_currentPathNode = 0;
	}
	
	
	private void FixedUpdate ()
	{
		CheckTurning();
		CheckYield();
		CheckSteerAngle();
		CheckBraking();
		SmoothSteer();
		if (!StopVehicle)
		{
			Move();
		}
		UpdateWaypoint();
	}


	// stop vehicle
	public void Stop()
	{
		_isBraking = true;
		_brakeTorqueConstant = MaxBrakeTorque;
	}

	// move vehicle
	public void Continue()
	{
		_isBraking = false;
		_brakeTorqueConstant = 0;
	}
	
	
	// check whether the vehicle needs to yield at turn
	private void CheckYield()
	{
		// stop at red or amber light
		
		// if joining a junction then yield
		
	}

	// functionality for moving the car
	private void Move()
	{
		_currentSpeed = 2 * Mathf.PI * WheelFrontLeft.radius * WheelFrontLeft.rpm * 60 / 1000;
		if (_currentSpeed < _speedConstant && (!_isBraking || !StopVehicle))
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


	// update to next node on path
	private void UpdateWaypoint()
	{
		if(transform.position.y < - 30) GetComponentInParent<TrafficDensity>().Despawn(gameObject);
		if (_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad
		    && Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) < 6)
		{
//			_isChangingPath = true;
		}
		if ((Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) > DistanceFromWpToChange)) return;

		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad) {
			_currentPathNode++;
//			_isSafeToChangePath = false;
//			_isChangingPath = false;
			_leftCross = false;
			_rightCross = false;
			_leftJunctionJoin = false;
			_rightJunctionJoin = false;
			_leftJunctionLeave = false;
			_rightJunctionCrossing = false;
			_isGoingStraightAtCross = false;
			_isGoingStraightAtJunction = false;
		} 
	}


	// build array of Waypoint transforms to follow
	public void BuildNextPath()
	{
		_previousRoad = _currentRoad;
		_currentRoad = _nextRoad;
		_nextRoad = null;
		if (_currentRoad == null)
		{
			print(gameObject.name);
			Debug.Break();
		}
		Transform[] pathTransforms = _currentRoad.GetComponentsInChildren<Transform>();
		_pathNodes = new List<Transform>();
		foreach(Transform waypoint in pathTransforms){
			if (_currentRoad.transform != waypoint)
			{
				_pathNodes.Add(waypoint);
			}
		}
		_currentPathNode = 0;
	}
	
	
	// get the next road coming up
	public void SetNextRoad()
	{
		Dictionary<String, Transform> dict = _currentRoad.GetComponent<WaypointPath>().GetNextRandomWaypointPath();
		String[] roadChoice = dict.Keys.ToArray();
		_nextRoad = dict[roadChoice[0]];
		_leftCross = false;
		_rightCross = false;
		_leftJunctionJoin = false;
		_rightJunctionJoin = false;
		_leftJunctionLeave = false;
		_rightJunctionCrossing = false;
		_isGoingStraightAtCross = false;
		_isGoingStraightAtJunction = false;
		switch (roadChoice[0])
		{
			case "straight-cross":
				_isGoingStraightAtCross = true;
				break;
			case "left-cross":
				_leftCross = true;
				break;
			case "right-cross":
				_rightCross = true;
				break;
			case "straight-junction":
				_isGoingStraightAtJunction = true;
				break;
			case "left-junction-join":
				_leftJunctionJoin = true;
				break;
			case "right-junction-join":
				_rightJunctionJoin = true;
				break;
			case "left-junction-leave":
				_leftJunctionLeave = true;
				break;
			case "right-junction-crossing":
				_rightJunctionCrossing = true;
				break;
			case "despawn":
				GetComponentInParent<TrafficDensity>().Despawn(gameObject);
				break;
			default: 
				GetComponentInParent<TrafficDensity>().Despawn(gameObject);
				break;
		}
	}

	
	// functionality for car to brake
	private void CheckBraking()
	{
		WheelBackLeft.brakeTorque = _brakeTorqueConstant;
		WheelBackRight.brakeTorque = _brakeTorqueConstant;
		WheelFrontLeft.brakeTorque = _brakeTorqueConstant;
		WheelFrontRight.brakeTorque = _brakeTorqueConstant;
	}

	
	// check if the vehicle is turning
	private void CheckTurning()
	{
		_speedConstant = MaxSpeed;
		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsFirstOnRoad) return;
		if (_leftCross)
		{
			_speedConstant = 30f;
		}
		else if (_rightCross)
		{
			_speedConstant = MaxSpeed;
		}
		else if (_leftJunctionJoin)
		{
			_speedConstant = MaxTurningSpeed;
		}
		else if (_rightJunctionJoin)
		{
			_speedConstant = MaxTurningSpeed;
		}
		else if (_leftJunctionLeave)
		{
			_speedConstant = MaxTurningSpeed;
		}
		else if (_rightJunctionCrossing)
		{
			_speedConstant = MaxSpeed;
		}
		else if (_isGoingStraightAtCross || _isGoingStraightAtJunction)
		{
			_speedConstant = MaxSpeed;
		}
		else
		{
			_speedConstant = MaxTurningSpeed;
		}
	}
	
	
	// functionality to steer car in correct direction
	private void CheckSteerAngle()
	{
		Vector3 relativeVector = transform.InverseTransformPoint(_pathNodes[_currentPathNode].position);
		relativeVector /= relativeVector.magnitude;
		float angle = MaxSteerAngle + Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position);
		float turnAngle = (relativeVector.x / relativeVector.magnitude) * MaxSteerAngle;
		_targetSteerAngle = turnAngle;
	}
	
	
	// functionality to smooth steering
	private void SmoothSteer()
	{
		WheelFrontLeft.steerAngle = Mathf.Lerp(WheelFrontLeft.steerAngle, _targetSteerAngle, Time.deltaTime * _smoothTurningSpeed);
		WheelFrontRight.steerAngle = Mathf.Lerp(WheelFrontRight.steerAngle, _targetSteerAngle, Time.deltaTime * _smoothTurningSpeed);
	}
	
	
	// function to calculate relative brake torque
	// equation: BrakeTorque = 0.5 * Mass * Velocity^2 / distanceToStop
	private float CalculateBrakeTorque(float distance)
	{
		return
			0.5f * 
			_rigidbodyComponent.mass * 
				(
					(float) Math.Pow(_rigidbodyComponent.velocity.x, 2) +
					(float) Math.Pow(_rigidbodyComponent.velocity.y, 2) + 
					(float) Math.Pow(_rigidbodyComponent.velocity.z, 2)
				) / 
			distance;
	}


}
