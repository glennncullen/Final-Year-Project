using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

public class TrafficDensity : MonoBehaviour
{

	public List<Transform> VehiclesToSpawn = new List<Transform>(); 
	public List<Transform> RoadsThatSpawn = new List<Transform>();

	[Header("Traffic Attributes")] 
	public int Density = 15;
	public float SpawnRate = 10f;

	// private variables
	private int _currentDensity = 0; 
	
	// spawn cars at interval
	IEnumerator SpawnCars()
	{
		while (true)
		{
			if (_currentDensity < Density)
			{
				List<Transform> ShuffledRoads = new List<Transform>(RoadsThatSpawn);
				while (_currentDensity < Density && ShuffledRoads.Count > 0)
				{
					Transform road = ShuffledRoads[Random.Range(0, ShuffledRoads.Count - 1)];
					Transform vehicle = VehiclesToSpawn[Random.Range(0, VehiclesToSpawn.Count)];
					vehicle.GetComponent<CarAI>().StartingRoad = road;
					vehicle.GetComponent<CarAI>().ShowRaycast = true;
					Transform[] waypoints = road.GetComponent<WaypointPath>().GetWaypoints();
					Vector3 rotation = waypoints[waypoints.Length - 1].transform.position - waypoints[0].transform.position;
					Instantiate(vehicle, waypoints[0].transform.position, Quaternion.LookRotation(rotation), transform);
					_currentDensity++;
					ShuffledRoads.Remove(road);
				}
			}
			yield return new WaitForSeconds(SpawnRate);
		}
	}
	
	
	// despawn vehicle
	public void Despawn(GameObject vehicle)
	{
		Destroy(vehicle);
		_currentDensity--;
	}
	
	
	// Use this for initialization
	private void Start ()
	{
		StartCoroutine(SpawnCars());
	}

//	private void FixedUpdate()
//	{
//		if (SpawnRate < 2)
//		{
//			SpawnRate = 2;
//		}
//	}
	
}
