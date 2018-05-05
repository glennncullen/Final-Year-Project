using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaypointPath : MonoBehaviour {

	public Color LineColour;
	public Transform LeftTurn;
	public Transform StraightOn;
	public Transform RightTurn;

	[Header("Traffic Lights")] 
	public Transform TrafficLights;
	
	[Header("Line Attributes")]
	public float StartNodeSize = 1;
	public float EndNodeSize = 2;
	public float NodeSize = 0.5f;
	public Boolean AlwaysShowPath;
	
	// private variables
	private List<Transform> _nodes = new List<Transform>();

	// set first and last nodes on Waypoint path
	private void Awake()
	{
		Transform[] pathTransforms = GetComponentsInChildren<Transform> ();
		_nodes = new List<Transform>();
		foreach(Transform pathTransform in pathTransforms){
			if(pathTransform != transform){
				_nodes.Add (pathTransform);
			}
		}
		for(int i = 0; i < _nodes.Count; i++){
			if (i == 0)
			{
				_nodes[i].GetComponent<Waypoint>().IsFirstOnRoad = true;
				_nodes[i].GetComponent<Waypoint>().IsLastOnRoad = false;
			}
			else if (i == _nodes.Count - 1)
			{
				_nodes[i].GetComponent<Waypoint>().IsFirstOnRoad = false;
				_nodes[i].GetComponent<Waypoint>().IsLastOnRoad = true;
			}
			else
			{
				_nodes[i].GetComponent<Waypoint>().IsFirstOnRoad = false;
				_nodes[i].GetComponent<Waypoint>().IsLastOnRoad = false;
			}
		}
	}

	// if AlwaysShowPath is true, show path at all times
	private void OnDrawGizmos()
	{
		if (AlwaysShowPath)
		{
			DrawPath();
		}
	}

	// make path visible when selected
	private void OnDrawGizmosSelected()
	{
		if (!AlwaysShowPath)
		{
			DrawPath();
		}
	}

	// create lines and spheres of path
	public void DrawPath()
	{
		Gizmos.color = LineColour;
		Transform[] pathTransforms = GetComponentsInChildren<Transform> ();
		_nodes = new List<Transform>();

		foreach(Transform pathTransform in pathTransforms){
			if(pathTransform != transform){
				_nodes.Add (pathTransform);
			}
		}

		for(int i = 0; i < _nodes.Count; i++){
			Vector3 currentNode = _nodes [i].position;
			Vector3 previousNode;
			if (i > 0) {
				previousNode = _nodes [i - 1].position;
				Gizmos.DrawLine (currentNode, previousNode);
			} 
			if (i == 0)
			{
				_nodes[i].GetComponent<Waypoint>().IsFirstOnRoad = true;
				_nodes[i].GetComponent<Waypoint>().IsLastOnRoad = false;
				Gizmos.DrawSphere(currentNode, StartNodeSize);
			}
			else if (i == _nodes.Count - 1)
			{
				_nodes[i].GetComponent<Waypoint>().IsFirstOnRoad = false;
				_nodes[i].GetComponent<Waypoint>().IsLastOnRoad = true;
				Gizmos.DrawSphere(currentNode, EndNodeSize);
			}
			else
			{
				_nodes[i].GetComponent<Waypoint>().IsFirstOnRoad = false;
				_nodes[i].GetComponent<Waypoint>().IsLastOnRoad = false;
				Gizmos.DrawWireSphere (currentNode, NodeSize);
			}
		}
	}


	// return the first waypoint in the waypoint path
	public Transform[] GetWaypoints()
	{
		Transform[] pathTransforms = GetComponentsInChildren<Transform> ();
		_nodes = new List<Transform>();
		foreach(Transform pathTransform in pathTransforms){
			if(pathTransform != transform)
			{
				_nodes.Add(pathTransform);
			}
		}
		return _nodes.ToArray();
	}
	
	
	// return a random connected road
	public Dictionary<String, Transform> GetNextRandomWaypointPath()
	{
		Dictionary<String, Transform> paths = new Dictionary<String, Transform>();
		if (LeftTurn != null && RightTurn != null && StraightOn != null) // at a cross section
		{
			paths.Add("left-cross", LeftTurn);
			paths.Add("right-cross", RightTurn);
			paths.Add("straight", StraightOn);
		}
		else if (LeftTurn != null && RightTurn != null && StraightOn == null) // joining road at T junction
		{
			paths.Add("left-junction-join", LeftTurn);
			paths.Add("right-junction-join", RightTurn);
		}
		else if (LeftTurn != null && RightTurn == null && StraightOn != null) // taking a left or going straight at T junction
		{
			paths.Add("left-junction-leave", LeftTurn);
			paths.Add("straight", StraightOn);
		}
		else if (LeftTurn == null && RightTurn != null && StraightOn != null) // taking a right or going straight at T junction
		{
			paths.Add("right-junction-crossing", RightTurn);
			paths.Add("straight", StraightOn);
		}
		else
		{
			paths.Add("despawn", null);
		}
		String[] s = paths.Keys.ToArray();
		String choice = s[Random.Range(0, paths.Count)];
		Dictionary<String, Transform> toReturn = new Dictionary<String, Transform>();
		toReturn.Add(choice, paths[choice]);
		return toReturn;
	}
	
}
