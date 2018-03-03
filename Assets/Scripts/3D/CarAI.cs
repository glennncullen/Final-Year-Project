using System.Collections.Generic;
using UnityEngine;

namespace _3D
{
	public class CarAi : MonoBehaviour {
	
		private Rigidbody _rigidbodyComponent;
		public float MaxSpeed = 80f;
		public float MaxTorque = 100f;
		public float MaxBrakeTorque = 150f;
		public Vector3 CenterOfMass;
		private float _currentSpeed;
		public bool IsBraking = false;
	
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
		public Vector3 FrontSensorPosition = new Vector3(0f, 0.25f, 0.75f);
		public float SideSensorPosition = 0.3f;
		public float FrontSensorAngle = 30f;
		private bool _isAvoiding;
		private float _avoidMultiplier = 0;

		// Use this for initialization
		private void Start () {
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
			if (_isAvoiding) return;
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
			if (!(Vector3.Distance(transform.position, _nodes[_currentNode].position) < 0.5f)) return;
			if (_currentNode != _nodes.Count - 1) {
				_currentNode++;
			} else {
				_currentNode = 0;
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
			_isAvoiding = false;
		
			// front center sensor
			if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, SensorLength))
			{
				if (hit.collider.GetComponent<TerrainCollider>() == null)
				{
					_isAvoiding = true;
				}
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
			}
		
			// front right sensor
			sensorOriginPosition += transform.right * SideSensorPosition;
			if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, SensorLength))
			{
				if (hit.collider.GetComponent<TerrainCollider>() == null)
				{
					_isAvoiding = true;
					_avoidMultiplier -= 1f;
				}
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
			}
			// front right-angle sensor
			else if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(FrontSensorAngle, transform.up) * transform.forward, out hit, SensorLength))
			{
				if (hit.collider.GetComponent<TerrainCollider>() == null)
				{
					_isAvoiding = true;
					_avoidMultiplier -= 0.5f;
				}
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
			}
		
			// front left sensor
			sensorOriginPosition -= transform.right * SideSensorPosition * 2;
			if (Physics.Raycast(sensorOriginPosition, transform.forward, out hit, SensorLength))
			{
				if (hit.collider.GetComponent<TerrainCollider>() == null)
				{
					_isAvoiding = true;
					_avoidMultiplier += 1f;
				}
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
			}
			// front left-angle sensor
			else if (Physics.Raycast(sensorOriginPosition, Quaternion.AngleAxis(-FrontSensorAngle, transform.up) *transform.forward, out hit, SensorLength))
			{
				if (hit.collider.GetComponent<TerrainCollider>() == null)
				{
					_isAvoiding = true;
					_avoidMultiplier += 0.5f;
				}
				Debug.DrawLine(sensorOriginPosition, hit.point, Color.green);
			}

			if (_isAvoiding)
			{
				WheelFrontLeft.steerAngle = MaxSteerAngle * _avoidMultiplier;
				WheelFrontRight.steerAngle = MaxSteerAngle * _avoidMultiplier;
			}
		
		}
	}
}
