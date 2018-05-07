using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class CarAI : MonoBehaviour {
	
	[Header("Vehicle Properties")]
	public Vector3 CenterOfMass;
	public float MaxSpeed = 50f;
	public float MaxTorque = 1000f;
	public float MaxSteerAngle = 55f;
	public float MaxTurningSpeed = 40f;
	public bool StopVehicle = false;

	[Header("Wheel Colliders")]
	public WheelCollider WheelFrontLeft;
	public WheelCollider WheelFrontRight;
	public WheelCollider WheelBackLeft;
	public WheelCollider WheelBackRight;

	[Header("Colliders")] 
//	public BoxCollider FrontCollider;
//	public BoxCollider BackCollider;

	[Header("Route To Follow")]
	public Transform StartingRoad;
	public float DistanceFromWpToChange = 3f;

	[Header("Sensors")] 
	public float FrontSensorLength = 7.5f;

	public Vector3 FrontSensorPosition = new Vector3(0f, 1.5f, 3.7f);
	public float SideSensorPosition = 1f;
	public Vector3 SensorsToLookAhead = new Vector3(15f, 25f, 35f); // angle, angle, length
	public Vector4 SensorToLookRight = new Vector4(80f, 30f, 70f, 20f); // angle, length, angle, length
	public Vector4 SensorToLookLeft = new Vector4(-40f, 30f, -20f, 15f); // angle, length, angle, length

	// private variables
	private Rigidbody _rigidbodyComponent;
	private float _currentSpeed;
	private List<Transform> _pathNodes;
	private int _currentPathNode;
	private bool _isBraking;
	private float _targetSteerAngle;
	public float MaxBrakeTorque = 2000f;
	public bool ShowRaycast;
	private float _smoothTurningSpeed = 5f;
	public bool _isChangingPath;
	public bool _isSafeToChangePath;
	public bool _leftCross;
	public bool _rightCross;
	public bool _leftJunctionJoin;
	public bool _rightJunctionJoin;
	public bool _leftJunctionLeave;
	public bool _rightJunctionCrossing;
	public bool _isGoingStraightAtJunction;
	public bool _isGoingStraightAtCross;
	public bool BreakRedLight;
	public bool _filterLight;
	public bool WaitForCarBreakingRedLight;
	private float _speedConstant;
	private float _brakeTorqueConstant;
	private float _torqueConstant;
	private Transform _currentRoad;
	private Transform _nextRoad;
	private Transform _previousRoad;
	private Transform _trafficLights;
	private bool[] _lightsRedYellowGreen = new []{false, false, false};

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
		for(int i =  0; i < _lightsRedYellowGreen.Length; i++)
		{
			_lightsRedYellowGreen[i] = false;
		}
		
		
	}
	
	
	private void FixedUpdate ()
	{
		CheckTrafficLightState();
		CheckTurning();
		CheckYield();
		CheckSensors();
		CheckSteerAngle();
		CheckBraking();
		SmoothSteer();
		Move();
		UpdateWaypoint();
//		_isBraking = false;
//		_brakeTorqueConstant = 0;
		_torqueConstant = MaxTorque;
		WaitForCarBreakingRedLight = false;
	}


	// get the current state of the traffic lights facing the vehicle
	private void CheckTrafficLightState()
	{
		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad) return;
		_trafficLights = _currentRoad.GetComponent<WaypointPath>().TrafficLights;
		if (_trafficLights == null) return;
		_lightsRedYellowGreen = _trafficLights.GetComponent<TrafficLightControl>().GetTrafficLightsFacing();
	}
	
	
	// check if the vehicle is turning
	private void CheckTurning()
	{
		_speedConstant = MaxSpeed;
		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsFirstOnRoad) return;
		if (_leftCross)
		{
			_speedConstant = MaxTurningSpeed;
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

	
	// check whether the vehicle needs to yield at turn
	private void CheckYield()
	{
		// stop at red or amber light
		if ((_lightsRedYellowGreen[0] || _lightsRedYellowGreen[1]) && Vector3.Distance(transform.position, _pathNodes[_pathNodes.Count-1].position) < 7)
		{
			_isBraking = true;
			_brakeTorqueConstant = CalculateBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) + 0.2f);
		}
		// if joining a junction then yield
		if ((_leftJunctionJoin || _rightJunctionJoin || _rightJunctionCrossing) && !_isSafeToChangePath && Vector3.Distance(transform.position, _pathNodes[_pathNodes.Count-1].position) < 7)
		{
			_brakeTorqueConstant = CalculateBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) + 0.2f);
			_isBraking = true;
		}
		if (_trafficLights == null) return;
		// if the lights are yellow, previously green, and the vehicle is too close to the traffic lights, slam on the brakes
		if (_lightsRedYellowGreen[1] && _trafficLights.GetComponent<TrafficLightControl>().PreviousLightGreen &&
		    Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) < 3 && _isChangingPath)
		{
			_isBraking = true;
			_brakeTorqueConstant = MaxBrakeTorque;
		}
	}

	// functionality for moving the car
	private void Move()
	{
		_currentSpeed = 2 * Mathf.PI * WheelFrontLeft.radius * WheelFrontLeft.rpm * 60 / 1000;
		if (_torqueConstant < 0)
		{
			WheelFrontLeft.motorTorque = _torqueConstant;
			WheelFrontRight.motorTorque = _torqueConstant;
			print(_torqueConstant);
		}
		else if (_currentSpeed < _speedConstant && !_isBraking)
		{
			WheelFrontLeft.motorTorque = _torqueConstant;
			WheelFrontRight.motorTorque = _torqueConstant;
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
			_isChangingPath = true;
		}
		if (_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad
			&& Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) < 10
			&& _nextRoad == null)
		{
			GetNextRoad();
			return;
		}

		if (BreakRedLight)
		{
			BuildNextPath();
			return;
		}
		
		if ((Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) > DistanceFromWpToChange)) return;
		if (WaitForCarBreakingRedLight) return;

		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad) {
			_currentPathNode++;
			_filterLight = false;
			_isSafeToChangePath = false;
			_isChangingPath = false;
			_leftCross = false;
			_rightCross = false;
			_leftJunctionJoin = false;
			_rightJunctionJoin = false;
			_leftJunctionLeave = false;
			_rightJunctionCrossing = false;
			_isGoingStraightAtCross = false;
			_isGoingStraightAtJunction = false;
		} 
		else if ((!_lightsRedYellowGreen[0] && !_lightsRedYellowGreen[1] && _isSafeToChangePath))
		{
			BuildNextPath();
		}
	}


	// build array of Waypoint transforms to follow
	private void BuildNextPath()
	{
		_trafficLights = null;
		_lightsRedYellowGreen[0] = false;
		_lightsRedYellowGreen[1] = false;
		_lightsRedYellowGreen[2] = false;
		BreakRedLight = false;
		if(!_leftJunctionJoin || !_rightJunctionCrossing || !_rightJunctionJoin)_isSafeToChangePath = false;
		_previousRoad = _currentRoad;
		_currentRoad = _nextRoad;
		_nextRoad = null;
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
	private void GetNextRoad()
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
			case "straight-junction":
				_isGoingStraightAtJunction = true;
				break;
			case "left-cross":
				_leftCross = true;
				break;
			case "right-cross":
				_rightCross = true;
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
		if (StopVehicle)
		{
			_brakeTorqueConstant = MaxBrakeTorque;
		}
		WheelBackLeft.brakeTorque = _brakeTorqueConstant;
		WheelBackRight.brakeTorque = _brakeTorqueConstant;
		WheelFrontLeft.brakeTorque = _brakeTorqueConstant;
		WheelFrontRight.brakeTorque = _brakeTorqueConstant;
	}
	
	
	// check sensors for obstructions if necessary
	private void CheckSensors()
	{
		if (_isGoingStraightAtCross || _isGoingStraightAtJunction)
		{
			CheckFrontSensors();
		}

		if (_isSafeToChangePath) return;
		if (_isChangingPath && _rightCross && _trafficLights != null && !WaitForCarBreakingRedLight)
		{
			if (_trafficLights.GetComponent<TrafficLightControl>().PreviousLightGreen && _trafficLights.GetComponent<TrafficLightControl>().GetAllRed())
			{
				BreakRedLight = true;
				_filterLight = true;
				return;
			}
		}
		if (!_isChangingPath)
		{
			if (Vector3.Distance(transform.position, _pathNodes[0].position) < 3f)
			{
				FrontSensorLength = FrontSensorLength / 3;
				CheckFrontSensors();
				FrontSensorLength = FrontSensorLength * 3;
				return;
			}
			CheckFrontSensors();
		}
		else if (_isChangingPath && !_isSafeToChangePath)
		{
			if (_leftCross && _lightsRedYellowGreen[2])
			{
				CheckForTurningAhead();
				_isSafeToChangePath = true;
			}

			if (_leftJunctionLeave)
			{
				CheckForTurningAhead();
				_isSafeToChangePath = true;
			}

			if (_rightCross && _lightsRedYellowGreen[2])
			{
				LookAheadSensor();
			}

			if (_leftJunctionJoin)
			{
				SensorsToLookAhead.z = SensorsToLookAhead.z / 5;
				LookAheadSensor();
				if(!_isSafeToChangePath) return;
				SensorsToLookAhead.z = SensorsToLookAhead.z * 5;
				LookRightSensor();
			}

			if (_rightJunctionJoin)
			{
				LookAheadSensor();
				if(!_isSafeToChangePath) return;
				LookRightSensor();
				if(!_isSafeToChangePath) return;
				LookLeftSensor();
			}

			if (_rightJunctionCrossing)
			{
				LookAheadSensor();
				if(!_isSafeToChangePath) return;
			}

			if ((_isGoingStraightAtCross && _lightsRedYellowGreen[2]) || _isGoingStraightAtJunction) _isSafeToChangePath = true;
		}
	}
	
	
	// check if there is a car turning right up ahead
	private void CheckForTurningAhead()
	{
		RaycastHit hit;
		Vector3 sensorOriginPosition = transform.position;
		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
		sensorOriginPosition += transform.up * FrontSensorPosition.y;
		sensorOriginPosition += transform.right * SideSensorPosition;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength * 2))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (hit.transform.gameObject.GetComponent<CarAI>()._rightCross && hit.transform.gameObject.GetComponent<CarAI>()._isSafeToChangePath)
					{
						if (ShowRaycast)
						{
							Debug.DrawLine(sensorOriginPosition, hit.point, Color.gray);
						}
						_isBraking = true;
						_brakeTorqueConstant = MaxBrakeTorque;
						if (hit.transform.gameObject.GetComponent<CarAI>()._filterLight)
						{
							WaitForCarBreakingRedLight = true;
							return;
						}
					}
				}
			}
		}
		sensorOriginPosition -= transform.right * SideSensorPosition * 2;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength * 2))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (hit.transform.gameObject.GetComponent<CarAI>()._rightCross && hit.transform.gameObject.GetComponent<CarAI>()._isSafeToChangePath)
					{
						if (ShowRaycast)
						{
							Debug.DrawLine(sensorOriginPosition, hit.point, Color.gray);
						}
						_isBraking = true;
						_brakeTorqueConstant = MaxBrakeTorque;
						if (hit.transform.gameObject.GetComponent<CarAI>()._filterLight)
						{
							WaitForCarBreakingRedLight = true;
							return;
						}
					}
				}
			}
		}
	}
	
	
	// check sensors at front
	private void CheckFrontSensors()
	{
		RaycastHit hit;
		Vector3 sensorOriginPosition = transform.position;
		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
		sensorOriginPosition += transform.up * FrontSensorPosition.y;
		sensorOriginPosition += transform.right * SideSensorPosition;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.blue);
					}

					_isBraking = true;
					_brakeTorqueConstant = MaxBrakeTorque;
				}
			}
		}
		sensorOriginPosition -= transform.right * SideSensorPosition * 2;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
					}

					_isBraking = true;
					_brakeTorqueConstant = MaxBrakeTorque;
				}
			}
		}
	}

	
	// check ahead on same road but opposite lane sensor
	private void LookAheadSensor()
	{
		bool canMove = true;
		RaycastHit hit;
		Vector3 sensorOriginPosition = transform.position;
		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
		sensorOriginPosition += transform.up * FrontSensorPosition.y;
		sensorOriginPosition += transform.right * SideSensorPosition;
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorsToLookAhead.x, transform.up) * transform.forward, out hit, SensorsToLookAhead.z))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.blue);
					}

					if (hit.transform.gameObject.GetComponent<CarAI>()._leftJunctionLeave && _leftJunctionJoin)
					{
						_isSafeToChangePath = true;
						return;
					}
					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}

		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorsToLookAhead.y, transform.up) * transform.forward, out hit, SensorsToLookAhead.z))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.blue);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
					if (hit.transform.gameObject.GetComponent<CarAI>()._filterLight)
					{
						WaitForCarBreakingRedLight = true;
						return;
					}
				}
			}
		}

		if (_rightCross)
		{
			if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(20, transform.up) * transform.forward, out hit, 35))
			{
				if (hit.collider.GetComponent<TerrainCollider>() == null)
				{
					if (hit.transform.gameObject.GetComponent<CarAI>() != null)
					{
						if (ShowRaycast)
						{
							Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
						}
						if (hit.transform.gameObject.GetComponent<CarAI>()._rightCross && !hit.transform.gameObject.GetComponent<CarAI>()._filterLight && !hit.transform.gameObject.GetComponent<CarAI>()._isSafeToChangePath)
						{
							_isSafeToChangePath = true;
							return;
						}
					}
				}
			}
		}
		
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorsToLookAhead.y+10, transform.up) * transform.forward, out hit, SensorsToLookAhead.z))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.blue);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
					if (hit.transform.gameObject.GetComponent<CarAI>()._filterLight)
					{
						WaitForCarBreakingRedLight = true;
						return;
					}
				}
			}
		}
		
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorsToLookAhead.x-5, transform.up) * transform.forward, out hit, SensorsToLookAhead.z+10))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.blue);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
					if (hit.transform.gameObject.GetComponent<CarAI>()._filterLight)
					{
						WaitForCarBreakingRedLight = true;
					}
				}
			}
		}
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.blue);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}
		sensorOriginPosition -= transform.right * SideSensorPosition * 2;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, FrontSensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}

		_isSafeToChangePath = canMove;
	}

	
	// check right side sensor
	private void LookRightSensor()
	{
		bool canMove = true;
		RaycastHit hit;
		Vector3 sensorOriginPosition = transform.position;
		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
		sensorOriginPosition += transform.up * FrontSensorPosition.y;
		sensorOriginPosition += transform.right * SideSensorPosition;
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookRight.x, transform.up) * transform.forward, out hit, SensorToLookRight.y))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
					}
					
					if (hit.transform.gameObject.GetComponent<CarAI>()._leftJunctionLeave && _leftJunctionJoin)
					{
//						_isBraking = false;
						_isSafeToChangePath = true;
						return;
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}

		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookRight.z, transform.up) * transform.forward, out hit, SensorToLookRight.w))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.yellow);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}
		
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookRight.x + 10, transform.up) * transform.forward, out hit, SensorToLookRight.y + 5))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}
		
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookRight.z-10, transform.up) * transform.forward, out hit, SensorToLookRight.w-10))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.yellow);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}

		_isSafeToChangePath = canMove;
	}
	
	
	// check left side sensor
	private void LookLeftSensor()
	{
		bool canMove = true;
		RaycastHit hit;
		Vector3 sensorOriginPosition = transform.position;
		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
		sensorOriginPosition += transform.up * FrontSensorPosition.y;
		sensorOriginPosition -= transform.right * SideSensorPosition * 2;
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookLeft.x + 15, transform.up) * transform.forward, out hit, SensorToLookLeft.y))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.blue);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookLeft.x, transform.up) * transform.forward, out hit, SensorToLookLeft.y))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.magenta);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookLeft.z, transform.up) * transform.forward, out hit, SensorToLookLeft.w))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.yellow);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookLeft.z + 15, transform.up) * transform.forward, out hit, SensorToLookLeft.w))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookLeft.z + 35, transform.up) * transform.forward, out hit, SensorToLookLeft.w))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				if (hit.transform.gameObject.GetComponent<CarAI>() != null)
				{
					if (ShowRaycast)
					{
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
					}

					_brakeTorqueConstant = MaxBrakeTorque;
					_isBraking = true;
					canMove = false;
				}
			}
		}

		_isSafeToChangePath = canMove;
	}
	
	
	// functionality to steer car in correct direction
	private void CheckSteerAngle()
	{
		Vector3 relativeVector = transform.InverseTransformPoint(_pathNodes[_currentPathNode].position);
		relativeVector /= relativeVector.magnitude;
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
