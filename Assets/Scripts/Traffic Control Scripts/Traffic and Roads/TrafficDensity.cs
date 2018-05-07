using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using UnityEngine;

public class TrafficDensity : MonoBehaviour
{

	public List<Transform> VehiclesToSpawn = new List<Transform>(); 
	public SpawnPoint[] SpawnPoints;

	[Header("Traffic Attributes")] 
	public int Density = 15;
	public float SpawnRate = 10f;

	// private variables
	private int _currentDensity = 0;
	private int _name = 1;

	private void Awake()
	{
		SpawnPoints = GetComponentsInChildren<SpawnPoint>();
		StartCoroutine(SpawnCars());
	}

	// spawn cars at interval
	IEnumerator SpawnCars()
	{
		while (true)
		{
			if (_currentDensity < Density)
			{
				List<SpawnPoint> ShuffledRoads = SpawnPoints.ToList();
				while (_currentDensity < Density && ShuffledRoads.Count > 0)
				{
					SpawnPoint spawn = ShuffledRoads[Random.Range(0, ShuffledRoads.Count - 1)];
					if (spawn.IsOccupied)
					{
						ShuffledRoads.Remove(spawn);
						continue;
					}
					Transform vehicle = VehiclesToSpawn[Random.Range(0, VehiclesToSpawn.Count)];
					vehicle.GetComponent<VehicleBehaviour>().StartingRoad = spawn.Road;
					vehicle.name = "ID: " + _name;
					_name++;
					Transform[] waypoints = spawn.Road.GetComponent<WaypointPath>().GetWaypoints();
					Vector3 rotation = waypoints[waypoints.Length - 1].transform.position - waypoints[0].transform.position;
					Instantiate(vehicle, spawn.transform.position, Quaternion.LookRotation(rotation), transform);
					_currentDensity++;
					ShuffledRoads.Remove(spawn);
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
	
}
