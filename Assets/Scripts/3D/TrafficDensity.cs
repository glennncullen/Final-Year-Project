using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficDensity : MonoBehaviour
{

	public List<Transform> VehiclesToSpawn = new List<Transform>(); 
	public List<Transform> RoadsThatSpawn = new List<Transform>();
	public List<Transform> RoadsThatDespawn = new List<Transform>();

	[Header("Traffic Attributes")] 
	public int Density = 15;
	public float SpawnRate = 2f;

	// private variables
	private int _currentDensity = 0; 
	
	// spawn cars at interval
	IEnumerator SpawnCars()
	{
		while (true)
		{
			if (_currentDensity < Density)
			{
				Transform vehicle = VehiclesToSpawn[Random.Range(0, VehiclesToSpawn.Count)];
				Transform spawnPosition = RoadsThatSpawn[Random.Range(0, RoadsThatSpawn.Count)];
				Instantiate(vehicle, spawnPosition);
//				foreach(Transform spawnPoint in RoadsThatSpawn)
//				{
//					spawnPoint.GetComponentsInChildren<Waypoint>();
//				}
			}
			yield return new WaitForSeconds(SpawnRate);
		}
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
