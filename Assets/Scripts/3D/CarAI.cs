using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class CarAI : MonoBehaviour {
	
	[Header("Vehicle Properties")]
	public Vector3 CenterOfMass;
	public float MaxSpeed = 80f;
	public float MaxTorque = 100f;
	public float MaxBrakeTorque = 150f;
	public float TurnSpeed = 5f;
	public float MaxSteerAngle = 30f;
	public bool IsBraking = false;
	
	[Header("Wheel Colliders")]
	public WheelCollider WheelFrontLeft;
	public WheelCollider WheelFrontRight;
	public WheelCollider WheelBackLeft;
	public WheelCollider WheelBackRight;

	[Header("Route To Follow")]
	public Transform Road;
	public float DistanceFromWaypointToChange = 0.5f;
	
	[Header("Sensors")] 
	public float SensorLength = 5f;
	public Vector3 FrontSensorPosition = new Vector3(0f, 0.25f, 0.75f);
	public float SideSensorPosition = 0.3f;
	public float FrontSensorAngle = 30f;
	
	// private variables
	private Rigidbody _rigidbodyComponent;
	private float _currentSpeed;
	private List<Transform> _pathNodes;
	private int _currentPathNode;
	private bool _isAvoiding;
	private float _targetSteerAngle;
	

	// initialization
	private void Start () {
		_rigidbodyComponent = GetComponent<Rigidbody> ();
		_rigidbodyComponent.centerOfMass = CenterOfMass;
		Transform[] pathTransforms = Road.GetComponentsInChildren<Transform>();
		_pathNodes = new List<Transform>();
		foreach(Transform navPoint in pathTransforms){
//			if(navPoint.GetComponent<ERNavPoint>() != null){
//				_pathNodes.Add (navPoint);
//			}
			if (Road.transform != navPoint)
			{
				_pathNodes.Add(navPoint);
			}
		}
		_isAvoiding = false;
		_currentPathNode = 0;
	}
	
	
	// Update is called once per frame
	private void FixedUpdate ()
	{
		CheckSensors();
		CheckSteerAngle();
		Move();
		CheckCurrentNodeDistance();
		CheckBraking();
		SmoothSteer();
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
		if (_currentSpeed < MaxSpeed && !IsBraking)
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
		if ((Vector3.Distance(transform.position, _pathNodes[_currentPathNode].position) > DistanceFromWaypointToChange)) return;
		if (_currentPathNode != _pathNodes.Count - 1) {
			_currentPathNode++;
		} else {
			_currentPathNode = 0;
		}
	}
	
	
	// functionality for car to brake
	private void CheckBraking()
	{
		if (IsBraking)
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
		sensorOriginPosition += transform.forward * FrontSensorPosition.z;
		sensorOriginPosition += transform.up * FrontSensorPosition.y;
		float avoidMultiplier = 0f;
		_isAvoiding = false;
		
		// front right sensor
		sensorOriginPosition += transform.right * SideSensorPosition;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, SensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
				_isAvoiding = true;
				avoidMultiplier -= 1f;
			}
		}
		// front right-angle sensor
		else if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(FrontSensorAngle, transform.up) * transform.forward, out hit, SensorLength / 2))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
				_isAvoiding = true;
				avoidMultiplier -= 0.5f;
			}
		}
		
		// front left sensor
		sensorOriginPosition -= transform.right * SideSensorPosition * 2;
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, SensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
				_isAvoiding = true;
				avoidMultiplier += 1f;
			}
		}
		// front left-angle sensor
		else if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(-FrontSensorAngle, transform.up) *transform.forward, out hit, SensorLength / 2))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
				_isAvoiding = true;
				avoidMultiplier += 0.5f;
			}
		}
		
		// front center sensor
		if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, SensorLength))
		{
			if (hit.collider.GetComponent<TerrainCollider>() == null)
			{
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
				_isAvoiding = true;
				if (hit.normal.x < 0)
				{
					avoidMultiplier = -1;
				}
				else
				{
					avoidMultiplier = 1;
				}
			}
		}

		if (_isAvoiding)
		{
			_targetSteerAngle = MaxSteerAngle * avoidMultiplier;
		}
	}
	
	
	// functionality to smooth steering
	private void SmoothSteer()
	{
		WheelFrontLeft.steerAngle = Mathf.Lerp(WheelFrontLeft.steerAngle, _targetSteerAngle, Time.deltaTime * TurnSpeed);
		WheelFrontRight.steerAngle = Mathf.Lerp(WheelFrontRight.steerAngle, _targetSteerAngle, Time.deltaTime * TurnSpeed);
	}
}
