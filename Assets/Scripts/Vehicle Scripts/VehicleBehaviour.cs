using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using Traffic_Control_Scripts.Communication;
using UnityEngine;
using UnityEngine.Experimental.U2D;
using UnityEngine.Scripting.APIUpdating;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class VehicleBehaviour : MonoBehaviour {
	
	[Header("Vehicle Properties")]
	public Vector3 CenterOfMass = new Vector3(0f, -0.2f, 0f);
	public float MaxSteerAngle = 55f;
	public float MaxSpeed = 50f;
	public float MaxTorque = 1000f;
	private float MaxTurningSpeed = 40f;
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
	public float _speedConstant;
	public float _brakeTorqueConstant;
	
	// route control
	private List<Transform> _pathNodes;
	private int _currentPathNode;
	public Transform _currentRoad;
	public Transform NextRoad;
	private Transform _previousRoad;
	
	// vehicle attributes
	public bool _isBraking;
	public bool EmergencyBrake;
	private float _targetSteerAngle;
	public float _smoothTurningSpeed = 5f;
	
	
	// private variables
	private Rigidbody _rigidbodyComponent;
	
	public List<LightStopX> LightStopXs = new List<LightStopX>();
	public List<LightStopZ> LightStopZs = new List<LightStopZ>();
	public List<CrossLane> CrossLanes = new List<CrossLane>();
	public List<JunctionLane> JunctionLanes = new List<JunctionLane>();
	public List<Front> Fronts = new List<Front>();
	
//	[HideInInspector] 
	public bool LeftCross;
//	[HideInInspector] 
	public bool RightCross;
//	[HideInInspector] 
	public bool LeftJunctionJoin;
//	[HideInInspector] 
	public bool RightJunctionJoin;
//	[HideInInspector] 
	public bool LeftJunctionLeave;
//	[HideInInspector] 
	public bool RightJunctionCrossing;
//	[HideInInspector] 
	public bool IsGoingStraightAtJunction;
//	[HideInInspector] 
	public bool IsGoingStraightAtCross;
//	[HideInInspector] 
	public bool IsUnableToMove;
	

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
		float wheelRadiusOffset = 1.7f / WheelFrontLeft.radius;
		MaxSpeed = MaxSpeed / wheelRadiusOffset;
		MaxTorque = MaxTorque / wheelRadiusOffset;
		MaxTurningSpeed = MaxTurningSpeed / wheelRadiusOffset;
		WheelFrontLeft.mass = WheelFrontLeft.mass * wheelRadiusOffset;
		WheelBackLeft.mass = WheelBackLeft.mass * wheelRadiusOffset;
		WheelBackRight.mass = WheelBackRight.mass * wheelRadiusOffset;
		WheelFrontRight.mass = WheelFrontRight.mass * wheelRadiusOffset;
	}
	
	
	private void FixedUpdate ()
	{
		CheckTurningSpeed();
		CheckSteerAngle();
		if (Handler.IsSomethingOnFire && CompareTag("firebrigade")) EmergencyDriving();
		else _speedConstant = MaxSpeed;
		ReduceSpeed();
		CheckBraking();
		SmoothSteer();
		if (!StopVehicle) Move();
		UpdateWaypoint();
		if (!EmergencyBrake || Handler.IsSomethingOnFire) return;
		EmergencyBrake = false;
		Continue();
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
	
	

	// functionality for moving the car
	private void Move()
	{
		_currentSpeed = 2 * Mathf.PI * WheelFrontLeft.radius * WheelFrontLeft.rpm * 60 / 1000;
		if (_currentSpeed < _speedConstant && (!_isBraking || !StopVehicle || !EmergencyBrake))
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


	// update to next node on path reset turinig bools
	private void UpdateWaypoint()
	{
		if (transform.position.y < -30)
		{
			_currentRoad.GetComponent<WaypointPath>().DecreaseCongestion();
			GetComponentInParent<TrafficDensity>().Despawn(gameObject);
		}
		if ((Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) > DistanceFromWpToChange)) return;
		if (_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad) return;
		_currentPathNode++;
		LeftCross = false;
		RightCross = false;
		LeftJunctionJoin = false;
		RightJunctionJoin = false;
		LeftJunctionLeave = false;
		RightJunctionCrossing = false;
		IsGoingStraightAtCross = false;
		IsGoingStraightAtJunction = false;
		IsUnableToMove = false;
		if (Handler.IsSomethingOnFire && CompareTag("firebrigade")) Handler.InformMove();
	}
	
	
	
	// drive like someone driving to a fire
	private void EmergencyDriving()
	{
		if (IsGoingStraightAtCross)
		{
			if (_previousRoad != null)
			{
				if (_previousRoad.gameObject.name.Substring(0, 5) == _currentRoad.gameObject.name.Substring(0, 5) &&
				    _currentRoad.GetComponent<WaypointPath>().Congestion == 1)
				{
					_speedConstant = MaxSpeed * 2;
				}
				else
				{
					_speedConstant = MaxSpeed;	
				}
			}
			else
			{
				_speedConstant = MaxSpeed;
			}
		}
		else if(_currentRoad.GetComponent<WaypointPath>().Congestion == 1)
		{
			_speedConstant = MaxSpeed * 2;
		}
		else
		{
			_speedConstant = MaxSpeed;
		}
		if (Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) < 15 &&
		     Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) > 5)
		{
			if(_currentSpeed > 50 && 
			   _pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad &&
			   Handler.Instance.LookAhead().Substring(0, 5) != _currentRoad.gameObject.name.Substring(0, 5))
			{
				_brakeTorqueConstant = CalculateBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position));
			}

			if (_currentRoad.GetComponent<WaypointPath>().TrafficLights == null) return;
			if (_currentRoad.GetComponent<WaypointPath>().TrafficLights.GetAllRed())
			{
				_brakeTorqueConstant = CalculateBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position));
			}
		}
		else
		{
			_brakeTorqueConstant = _isBraking ? MaxBrakeTorque : 0f;
		}
	}
	
	
	// calculate the brake torque required depending on the distance to the stop
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


	// build array of Waypoint transforms to follow
	public void BuildNextPath()
	{
		_previousRoad = _currentRoad;
		_previousRoad.GetComponent<WaypointPath>().DecreaseCongestion();
		_currentRoad = NextRoad;
		_currentRoad.GetComponent<WaypointPath>().IncreaseCongestion();
		NextRoad = null;
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
		Dictionary<String, Transform> dict;
		dict = CompareTag("firebrigade") ? _currentRoad.GetComponent<WaypointPath>().GetNextRandomWaypointPathForFirebrigade() : _currentRoad.GetComponent<WaypointPath>().GetNextRandomWaypointPath();
		String[] roadChoice = dict.Keys.ToArray();
		NextRoad = dict[roadChoice[0]];
		LeftCross = false;
		RightCross = false;
		LeftJunctionJoin = false;
		RightJunctionJoin = false;
		LeftJunctionLeave = false;
		RightJunctionCrossing = false;
		IsGoingStraightAtCross = false;
		IsGoingStraightAtJunction = false;
		IsUnableToMove = false;
		switch (roadChoice[0])
		{
			case "straight-cross":
				IsGoingStraightAtCross = true;
				break;
			case "left-cross":
				LeftCross = true;
				break;
			case "right-cross":
				RightCross = true;
				break;
			case "straight-junction":
				IsGoingStraightAtJunction = true;
				break;
			case "left-junction-join":
				LeftJunctionJoin = true;
				break;
			case "right-junction-join":
				RightJunctionJoin = true;
				break;
			case "left-junction-leave":
				LeftJunctionLeave = true;
				break;
			case "right-junction-crossing":
				RightJunctionCrossing = true;
				break;
			case "despawn":
				GetComponentInParent<TrafficDensity>().Despawn(gameObject);
				break;
			case "cant-move":
				IsUnableToMove = true;
				break;
			default: 
				print("SetNextRoad switch statement:\t" + gameObject.name);
				Debug.Break();
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
	private void CheckTurningSpeed()
	{
		_speedConstant = MaxSpeed;
		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsFirstOnRoad) return;
		if (IsGoingStraightAtCross || IsGoingStraightAtJunction || RightJunctionCrossing || RightCross)
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

	
	// reduce speed of firebrigade when path is reached
	private void ReduceSpeed()
	{
		if (!CompareTag("firebrigade") || Handler.IsSomethingOnFire) return;
		if (!(_speedConstant > MaxSpeed + 5)) return;
		_isBraking = true;
		_brakeTorqueConstant = MaxBrakeTorque;
	}

	private void OnCollisionEnter(Collision other)
	{
		if (other.gameObject.GetComponent<CarFrontCollider>() != null) return;
		if (other.gameObject.GetComponent<CarBackCollider>() != null) return;
		VehicleBehaviour vehicle = other.gameObject.GetComponentInParent<VehicleBehaviour>();
		if(vehicle == null) return;
		_currentRoad.GetComponent<WaypointPath>().DecreaseCongestion();
		foreach (LightStopX lightStop in LightStopXs)
		{
			lightStop.RemoveVehicle(this);
		}

		foreach (LightStopZ lightStop in LightStopZs)
		{
			lightStop.RemoveVehicle(this);
		}

		foreach (CrossLane cross in CrossLanes)
		{
			cross.RemoveVehicle(this);
		}

		foreach (JunctionLane junction in JunctionLanes)
		{
			junction.RemoveVehicle(this);
		}

		foreach (Front front in Fronts)
		{
			front.RemoveVehicle(this);
		}

		print(gameObject.name + " collided on " + _currentRoad.gameObject.name);
		if (!CompareTag("firebrigade"))
		{
			GetComponentInParent<TrafficDensity>().Despawn(gameObject);
		}
	}
	
}
