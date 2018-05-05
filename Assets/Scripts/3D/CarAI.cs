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
	public float FrontSensorLength = 7.5f;
	public Vector3 FrontSensorPosition = new Vector3(0f, 1.5f, 3.7f);
	public float SideSensorPosition = 1f;
	public Vector3 SensorsToLookAhead = new Vector3(15f, 25f, 35f); // angle, angle, length
	public Vector4 SensorToLookRight = new Vector4(80f, 30f, 70f, 20f); // angle, length, angle, length
	public Vector4 SensorToLookLeft = new Vector4(-55f, 30f, -40f, 15f); // angle, length, angle, length
	
	// private variables
	private Rigidbody _rigidbodyComponent;
	private float _currentSpeed;
	private List<Transform> _pathNodes;
	private int _currentPathNode;
	private bool _isBraking;
	
	private float _targetSteerAngle;
	
	public float MaxBrakeTorque = 2000f;
	
	[Header("Debug Properties")]
	public bool ShowRaycast;
	public bool _isChangingPath;
	public bool _isSafeToChangePath;
	public bool _leftCross;
	public bool _rightCross;
	public bool _leftJunctionJoin;
	public bool _rightJunctionJoin;
	public bool _leftJunctionLeave;
	public bool _rightJunctionCrossing;
	public bool _isGoingStraight;
	public bool BreakRedLight;
	public bool WaitForCarBreakingRedLight;
	private float _speedConstant;
	private float _brakeTorqueConstant;
	private Transform _currentRoad;
	private Transform _nextRoad;
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
		Move();
		UpdateWaypoint();
		SmoothSteer();
		_isBraking = false;
		_brakeTorqueConstant = 0;
		WaitForCarBreakingRedLight = false;
	}


	// get the current state of the traffic lights facing the vehicle
	private void CheckTrafficLightState()
	{
		Transform trafficLights = _currentRoad.GetComponent<WaypointPath>().TrafficLights;
		if (trafficLights == null)
		{
			for(int i =  0; i < _lightsRedYellowGreen.Length; i++)
			{
				_lightsRedYellowGreen[i] = false;
			}
			return;
		}
		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad) return;
		_lightsRedYellowGreen = trafficLights.GetComponent<TrafficLightControl>().GetTrafficLightsFacing();
		// if the lights are yellow, previously green, and the vehicle is too close to the traffic lights, continue on to next path
		if (_lightsRedYellowGreen[1] && trafficLights.GetComponent<TrafficLightControl>().PreviousLightGreen &&
		    Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) < 3 && _nextRoad != null)
		{
			BreakRedLight = true;
		}
	}
	
	
	// check if the vehicle is turning
	private void CheckTurning()
	{
		if (_isGoingStraight) return;
		if (_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsFirstOnRoad)
		{
			_speedConstant = MaxTurningSpeed;
		}
		else
		{
			_speedConstant = MaxSpeed;
		}
	}

	
	// check whether the vehicle needs to yield at turn
	private void CheckYield()
	{
		if ((_lightsRedYellowGreen[0] || _lightsRedYellowGreen[1]) && Vector3.Distance(transform.position, _pathNodes[_pathNodes.Count-1].position) < 7)
		{
			_isBraking = true;
			_brakeTorqueConstant = CalculateBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) + 0.2f);
		}
		if (_nextRoad == null) return;
		if ((_leftJunctionJoin || _rightJunctionJoin || _rightJunctionCrossing || _rightCross) && !_isSafeToChangePath)
		{
			_brakeTorqueConstant = CalculateBrakeTorque(Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position));
			if(!_rightCross) _isBraking = true;
		}
	}

	// functionality for moving the car
	private void Move()
	{
		_currentSpeed = 2 * Mathf.PI * WheelFrontLeft.radius * WheelFrontLeft.rpm * 60 / 1000;
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
		if(transform.position.y < - 30) GetComponentInParent<TrafficDensity>().Despawn(gameObject);
		if (_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad
			&& Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) < 6
			&& _nextRoad == null)
		{
			GetNextRoad();
			_isChangingPath = true;
		}
		
		if ((Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) > DistanceFromWpToChange)) return;
		if (WaitForCarBreakingRedLight) return;

		if (!_pathNodes[_currentPathNode].GetComponent<Waypoint>().IsLastOnRoad) {
			_currentPathNode++;
			BreakRedLight = false;
			_isSafeToChangePath = false;
			_isChangingPath = false;
			_leftCross = false;
			_rightCross = false;
			_leftJunctionJoin = false;
			_rightJunctionJoin = false;
			_leftJunctionLeave = false;
			_rightJunctionCrossing = false;
			_isGoingStraight = false;
		} 
		else if (((!_lightsRedYellowGreen[0] && !_lightsRedYellowGreen[1]) || BreakRedLight) && _isSafeToChangePath)
		{
			BuildNextPath();
		}
	}


	// build array of Waypoint transforms to follow
	private void BuildNextPath()
	{
		_isSafeToChangePath = false;
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
		_isGoingStraight = false;
		switch (roadChoice[0])
		{
			case "straight":
				_isGoingStraight = true;
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
		if (_isSafeToChangePath) return;
		if (_rightCross && _isChangingPath && _lightsRedYellowGreen[0] && !WaitForCarBreakingRedLight)
		{
			_isSafeToChangePath = true;
			_rightCross = false;
		}
//		else if (_lightsRedYellowGreen[0])
//		{
//			_isSafeToChangePath = false;
//		}
		else if (!_isChangingPath)
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
			if (_leftCross || _leftJunctionLeave)
			{
				_isSafeToChangePath = true;
			}

			if (_rightCross)
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
				LookLeftSensor();
				if(!_isSafeToChangePath) return;
				LookAheadSensor();
				if(!_isSafeToChangePath) return;
				LookRightSensor();
			}

			if (_rightJunctionCrossing)
			{
				LookAheadSensor();
				if(!_isSafeToChangePath) return;
			}

			if (!_isGoingStraight) return;
			_isSafeToChangePath = true;
			CheckFrontSensors();
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
						Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
					}
					if (hit.transform.gameObject.GetComponent<CarAI>()._rightCross && _rightCross)
					{
//						_isBraking = false;
						_isSafeToChangePath = true;
						return;
					}

					if (hit.transform.gameObject.GetComponent<CarAI>()._leftJunctionLeave && (_leftJunctionJoin || _rightJunctionJoin))
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
					if (hit.transform.gameObject.GetComponent<CarAI>().BreakRedLight)
					{
						WaitForCarBreakingRedLight = true;
						return;
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
					if (hit.transform.gameObject.GetComponent<CarAI>().BreakRedLight)
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
					if (hit.transform.gameObject.GetComponent<CarAI>().BreakRedLight)
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
					
					if (hit.transform.gameObject.GetComponent<CarAI>()._leftJunctionLeave && (_leftJunctionJoin || _rightJunctionJoin))
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
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookLeft.x, transform.up) * transform.forward, out hit, SensorToLookLeft.y))
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
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookLeft.z, transform.up) * transform.forward, out hit, SensorToLookLeft.w))
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
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookLeft.z + 20, transform.up) * transform.forward, out hit, SensorToLookLeft.w))
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
		if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(SensorToLookLeft.z + 50, transform.up) * transform.forward, out hit, SensorToLookLeft.w))
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
		WheelFrontLeft.steerAngle = Mathf.Lerp(WheelFrontLeft.steerAngle, _targetSteerAngle, Time.deltaTime * SmoothTurningSpeed);
		WheelFrontRight.steerAngle = Mathf.Lerp(WheelFrontRight.steerAngle, _targetSteerAngle, Time.deltaTime * SmoothTurningSpeed);
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
