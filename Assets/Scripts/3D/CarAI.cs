using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class CarAI : MonoBehaviour {
	
	[Header("Vehicle Properties")]
	public Vector3 CenterOfMass;
	public float MaxSpeed = 50f;
	public float MaxTorque = 80f;
//	public float MaxBrakeTorque = 150f;
	public float MaxSteerAngle = 55f;
	public float MaxTurningSpeed = 40f;
	public float SmoothTurningSpeed = 5f;
	public bool StopVehicle = false;

	[Header("Wheel Colliders")]
	public WheelCollider WheelFrontLeft;
	public WheelCollider WheelFrontRight;
	public WheelCollider WheelBackLeft;
	public WheelCollider WheelBackRight;

	[Header("Route To Follow")]
	public Transform StartingRoad;
	public float DistanceFromWpToChange = 3f;

	[Header("Sensors")] 
	public float FrontSensorLength = 20f;
	public float SideSensorLength = 20f;
	public Vector3 FrontSensorPosition = new Vector3(0f, 1.5f, 3.7f);
	public float SideSensorPosition = 1.5f;
	public float FrontSensorAngle = 45f;
	
	// private variables
	private Rigidbody _rigidbodyComponent;
	private float _currentSpeed;
	private List<Transform> _pathNodes;
	private int _currentPathNode;
	private bool _isBraking;
//	private bool _hardBrake;
	
	private bool _isAvoiding;
//	private bool _isTooCloseToVehicleInFront;
//	private bool _isUnsafeToTurn;
	
	private float _targetSteerAngle;
	private bool _isChangingPath;
	private float _speedConstant;
	private float _brakeTorqueConstant;
	private Transform _currentRoad;
	private bool[] _lightsRedYellowGreen = new []{false, false, false};
	private bool[] _lightsPreviousRedGreen = new[] {false, false};

	// initialization
	private void Start () {
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
		
		_isAvoiding = false;
//		_isTooCloseToVehicleInFront = false;
//		_isUnsafeToTurn = false;
		_speedConstant = MaxSpeed;
		_currentPathNode = 0;
		for(int i =  0; i < _lightsRedYellowGreen.Length; i++)
		{
			_lightsRedYellowGreen[i] = false;
		}
	}
	
	
	// build array of Waypoint transforms to follow
	private void BuildNextPath()
	{
		_currentRoad = _currentRoad.GetComponent<WaypointPath>().GetNextRandomWaypointPath();
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
	
	
	private void FixedUpdate ()
	{
//		if (_isChangingPath)
//		{
//			CheckSensors();
//		}
//		CheckSensors();
		CheckTrafficLightState();
		CheckFrontSensors();
		CheckTurning();
		CheckSteerAngle();
		UpdateWaypoint();
		CheckBraking();
		Move();
		SmoothSteer();
//		if (gameObject.CompareTag("Test Truck"))
//		{
//			print("brake:\t" + _isBraking);
//		}
		_isBraking = false;
//		_hardBrake = false;
		_speedConstant = MaxSpeed;
		
	}
	
	
	// get the current state of the traffic lights facing the vehicle
	private void CheckTrafficLightState()
	{
		Transform trafficLights = _currentRoad.GetComponent<WaypointPath>().TrafficLights; 
		if (trafficLights == null) return;
		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad) return;
		_lightsRedYellowGreen = trafficLights.GetComponent<TrafficLightControl>().GetTrafficLightsFacing();
//		if (_lightsRedYellowGreen[0] && (Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) < 8))
//		{
//			_hardBrake = true;
//		}
		if (_lightsRedYellowGreen[0] && (Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) < 15))
		{
			_isBraking = true;
			_brakeTorqueConstant = SetBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position));
		}
	}
	

	// functionality to steer car in correct direction
	private void CheckSteerAngle()
	{
		if (_isAvoiding) return;
		Vector3 relativeVector = transform.InverseTransformPoint(_pathNodes[_currentPathNode].position);
		relativeVector /= relativeVector.magnitude;
		float turnAngle = (relativeVector.x / relativeVector.magnitude) * MaxSteerAngle;
		_targetSteerAngle = turnAngle;
	}
	
	
	// functionality for moving the car
	private void Move()
	{
		_currentSpeed = 2 * Mathf.PI * WheelFrontLeft.radius * WheelFrontLeft.rpm * 60 / 1000;
//		if (_isChangingPath && _currentSpeed > MaxTurningSpeed)
//		{
//			_isBraking = true;
//		}
//		else if (_isChangingPath && _currentSpeed < MaxTurningSpeed)
//		{
////			_isBraking = false;
//			WheelFrontLeft.motorTorque = MaxTorque;
//			WheelFrontRight.motorTorque = MaxTorque;
//		}
//		else 
		if (_currentSpeed < _speedConstant && !_isBraking)
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
		if ((Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) > DistanceFromWpToChange)) return;

		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad) {
			_currentPathNode++;
		} 
		else if (!_lightsRedYellowGreen[0])
		{ // changing path
			BuildNextPath();
			_isChangingPath = true;
		}
	}


	// check if the vehicle is turning
	private void CheckTurning()
	{
		
		if (_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsFirstOnRoad)
		{
			_isChangingPath = true;
			_speedConstant = MaxTurningSpeed;
		}
		else
		{
			_isChangingPath = false;
		}
	}
	
	// functionality for car to brake
	private void CheckBraking()
	{
//		if (_hardBrake)
//		{
//			WheelBackLeft.brakeTorque = SetBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position));
//			WheelBackRight.brakeTorque = SetBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position));
//			WheelFrontLeft.brakeTorque = SetBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position));
//			WheelFrontRight.brakeTorque = SetBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position));
//		}
//		else 
		if (_isBraking)
		{
			WheelBackLeft.brakeTorque = _brakeTorqueConstant;
			WheelBackRight.brakeTorque = _brakeTorqueConstant;
			WheelFrontLeft.brakeTorque = _brakeTorqueConstant;
			WheelFrontRight.brakeTorque = _brakeTorqueConstant;
		}
		else
		{
			WheelBackLeft.brakeTorque = 0;
			WheelBackRight.brakeTorque = 0;
			WheelFrontLeft.brakeTorque = 0;
			WheelFrontRight.brakeTorque = 0;
		}
	}
	
	
//	// functionality to check sensors
//	private void CheckSensors()
//	{
//		RaycastHit hit;
//		Vector3 sensorOriginPosition = transform.position;
//		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
//		sensorOriginPosition += transform.up * FrontSensorPosition.y;
//		float avoidMultiplier = 0f;
//		_isAvoiding = false;
//		
//		// front right sensor
//		sensorOriginPosition += transform.right * SideSensorPosition;
//		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
//		{
//			if (hit.collider.GetComponent<TerrainCollider>() == null)
//			{
//				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isAvoiding = true;
//				avoidMultiplier -= 1f;
//			}
//		}
//		// front right-angle sensor
//		else if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(FrontSensorAngle, transform.up) * transform.forward, out hit, SideSensorLength / 2))
//		{
//			if (hit.collider.GetComponent<TerrainCollider>() == null)
//			{
//				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isAvoiding = true;
//				avoidMultiplier -= 0.5f;
//			}
//		}
//		
//		// front left sensor
//		sensorOriginPosition -= transform.right * SideSensorPosition * 2;
//		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
//		{
//			if (hit.collider.GetComponent<TerrainCollider>() == null)
//			{
//				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isAvoiding = true;
//				avoidMultiplier += 1f;
//			}
//		}
//		// front left-angle sensor
//		else if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(-FrontSensorAngle, transform.up) *transform.forward, out hit, SideSensorLength / 2))
//		{
//			if (hit.collider.GetComponent<TerrainCollider>() == null)
//			{
//				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isAvoiding = true;
//				avoidMultiplier += 0.5f;
//			}
//		}
//		
//		// front center sensor
//		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
//		{
//			if (hit.collider.GetComponent<TerrainCollider>() == null)
//			{
//				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isAvoiding = true;
//				if (hit.normal.x < 0)
//				{
//					avoidMultiplier = -1;
//				}
//				else
//				{
//					avoidMultiplier = 1;
//				}
//			}
//		}
//
//		if (_isAvoiding)
//		{
//			_targetSteerAngle = MaxSteerAngle * avoidMultiplier;
//		}
//	}
//	
	
	// check proximity to any vehicles in front and brake if too close
	
	private void CheckFrontSensors()
	{
		RaycastHit hit;
		Vector3 sensorOriginPosition = transform.position;
		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
		sensorOriginPosition += transform.up * FrontSensorPosition.y;
//		_isTooCloseToVehicleInFront = false;
		// front right sensor
		sensorOriginPosition += transform.right * SideSensorPosition;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
//				_isTooCloseToVehicleInFront = true;
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					Debug.DrawLine(sensorOriginPosition, hit.point, Color.blue);
					_speedConstant = hit.transform.gameObject.GetComponent<CarAI>()._currentSpeed;
					if (_speedConstant < 3 || hit.distance < 7)
					{
						_isBraking = true;
						_brakeTorqueConstant = SetBrakeTorque(hit.distance - 1);
					}
//					if (hit.distance < FrontSensorLength / 3 && _speedConstant < 3)
//					{
//						_hardBrake = true;
//					}
//					if (hit.distance < FrontSensorLength / 4)
//					{
//						_hardBrake = true;
//					}
				}
			}
		}
		// front left sensor
		sensorOriginPosition -= transform.right * SideSensorPosition * 2;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
//				_isTooCloseToVehicleInFront = true;
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
					_speedConstant = hit.transform.gameObject.GetComponent<CarAI>()._currentSpeed;
					if (_speedConstant < 3 || hit.distance < 7)
					{
						_isBraking = true;
						_brakeTorqueConstant = SetBrakeTorque(hit.distance - 1);
					}
//					if (hit.distance < FrontSensorLength / 3 && _speedConstant < 3)
//					{
//						_hardBrake = true;
//					}
//					if (hit.distance < FrontSensorLength / 4)
//					{
//						_hardBrake = true;
//					}
				}
			}
		}
		// front center sensor
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
//				_isTooCloseToVehicleInFront = true;
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
					_speedConstant = hit.transform.gameObject.GetComponent<CarAI>()._currentSpeed;
					if (_speedConstant < 3 || hit.distance < 7)
					{
						_isBraking = true;
						_brakeTorqueConstant = SetBrakeTorque(hit.distance - 1);
					}
//					if (hit.distance < FrontSensorLength / 3 && _speedConstant < 3)
//					{
//						_hardBrake = true;
//					}
//					if (hit.distance < FrontSensorLength / 4)
//					{
//						_hardBrake = true;
//					}
				}
			}
		}
//		_isBraking = _isTooCloseToVehicleInFront;
	}

	// check left side sensor to make sure coast is clear
	private void CheckLeftSideSensor()
	{
		RaycastHit hit;
		Vector3 sensorOriginPosition = transform.position;
		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
		sensorOriginPosition += transform.up * FrontSensorPosition.y;
//		_isUnsafeToTurn = false;
		// front left-angle sensor
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(-FrontSensorAngle, transform.up) *transform.forward, out hit, SideSensorLength / 2))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isUnsafeToTurn = true;
				_brakeTorqueConstant = SetBrakeTorque(Vector3.Distance(transform.position, hit.point - Vector3.one));
				_isBraking = true;
			}
		}
//		_isBraking = _isUnsafeToTurn;
	}
	
	// check right side sensor to make sure coast is clear
	private void CheckRightSideSensor()
	{
		RaycastHit hit;
		Vector3 sensorOriginPosition = transform.position;
		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
		sensorOriginPosition += transform.up * FrontSensorPosition.y;
//		_isUnsafeToTurn = false;
		// front right-angle sensor
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(FrontSensorAngle, transform.up) * transform.forward, out hit, SideSensorLength / 2))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isUnsafeToTurn = true;
				_brakeTorqueConstant = SetBrakeTorque(Vector3.Distance(transform.position, hit.point - Vector3.one));
				_isBraking = true;
			}
		}
//		_isBraking = _isUnsafeToTurn;
	}
	
	// functionality to smooth steering
	private void SmoothSteer()
	{
		WheelFrontLeft.steerAngle = Mathf.Lerp(WheelFrontLeft.steerAngle, _targetSteerAngle, Time.deltaTime * SmoothTurningSpeed);
		WheelFrontRight.steerAngle = Mathf.Lerp(WheelFrontRight.steerAngle, _targetSteerAngle, Time.deltaTime * SmoothTurningSpeed);
	}
	
	// function to calculate relative brake torque
	// equation: BrakeTorque = 0.5 * Mass * Velocity^2 / distanceToStop
	private float SetBrakeTorque(float distance)
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
