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
	public float MaxBrakeTorque = 150f;
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
	private bool _hardBrake;
	
	private bool _isAvoiding;
//	private bool _isTooCloseToVehicleInFront;
//	private bool _isUnsafeToTurn;
	
	private float _targetSteerAngle;
	private bool _isChangingPath;
	private float _speedConstant;
	private Transform _currentRoad;

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
		CheckTurning();
		CheckFrontSensors();
		CheckSteerAngle();
		UpdateWaypoint();
		CheckBraking();
		Move();
		SmoothSteer();
//		if (gameObject.CompareTag("Test Truck"))
//		{
//			print(_speedConstant);
//		}
		_isBraking = false;
		_hardBrake = false;
		_speedConstant = MaxSpeed;
//		print("Is Braking:\t" + IsBraking + "\tSpeed:\t" + _currentSpeed + "\nChange Path\t" + _isChangingPath);
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
		} else { // changing path
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
//			_isBraking = false;
			_isChangingPath = false;
		}
	}
	
	// functionality for car to brake
	private void CheckBraking()
	{
		if (_hardBrake)
		{
			WheelBackLeft.brakeTorque = MaxBrakeTorque;
			WheelBackRight.brakeTorque = MaxBrakeTorque;
			WheelFrontLeft.brakeTorque = MaxBrakeTorque;
			WheelFrontRight.brakeTorque = MaxBrakeTorque;
		}
		else if (_isBraking || StopVehicle)
		{
			WheelBackLeft.brakeTorque = MaxBrakeTorque;
			WheelBackRight.brakeTorque = MaxBrakeTorque;
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
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isTooCloseToVehicleInFront = true;
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					_speedConstant = hit.transform.gameObject.GetComponent<CarAI>()._currentSpeed;
					if (_speedConstant < 3)
					{
						_isBraking = true;
					}
					if (hit.distance < FrontSensorLength / 3 && _speedConstant < 3)
					{
						_hardBrake = true;
					}
				}
			}
		}
		// front left sensor
		sensorOriginPosition -= transform.right * SideSensorPosition * 2;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isTooCloseToVehicleInFront = true;
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					_speedConstant = hit.transform.gameObject.GetComponent<CarAI>()._currentSpeed;
					if (_speedConstant < 3)
					{
						_isBraking = true;
					}
					if (hit.distance < FrontSensorLength / 3 && _speedConstant < 3)
					{
						_hardBrake = true;
					}
				}
			}
		}
		// front center sensor
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
//				_isTooCloseToVehicleInFront = true;
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					_speedConstant = hit.transform.gameObject.GetComponent<CarAI>()._currentSpeed;
					if (_speedConstant < 3)
					{
						_isBraking = true;
					}
					if (hit.distance < FrontSensorLength / 3 && _speedConstant < 3)
					{
						_hardBrake = true;
					}
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
}
