using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaypointPath : MonoBehaviour {

	public Color LineColour;
	public Transform LeftTurn;
	public Transform StraightOn;
	public Transform RightTurn;

	[Header("Line Attributes")]
	public float StartNodeSize = 1;
	public float EndNodeSize = 2;
	public float NodeSize = 0.5f;
	public Boolean AlwaysShowPath;

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

	// return a random connected road
	public Transform GetNextRandomWaypointPath()
	{
		List<Transform> paths = new List<Transform>();
		if (LeftTurn != null)
		{
			paths.Add(LeftTurn);
		}
		if (RightTurn != null)
		{
			paths.Add(RightTurn);
		}
		if (StraightOn != null)
		{
			paths.Add(StraightOn);
		}
		return paths[Random.Range(0, paths.Count)];
	}
	
}
