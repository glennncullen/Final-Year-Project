using System;
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Crypto.Engines;
using Traffic_Control_Scripts.Communication;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaypointPath : MonoBehaviour
{

	public Color LineColour;
	public Transform LeftTurn;
	public Transform StraightOn;
	public Transform RightTurn;

	[Header("Traffic Lights")] public TrafficLightControl TrafficLights;
	public int CongestionThreshold = 7;

	[Header("Line Attributes")] public float StartNodeSize = 1;
	public float EndNodeSize = 2;
	public float NodeSize = 0.5f;
	public Boolean AlwaysShowPath;

	// private variables
	private List<Transform> _nodes = new List<Transform>();
	private int _congestion = 0;

	// set first and last nodes on Waypoint path
	private void Awake()
	{
		Transform[] pathTransforms = GetComponentsInChildren<Transform>();
		_nodes = new List<Transform>();
		foreach (Transform pathTransform in pathTransforms)
		{
			if (pathTransform != transform)
			{
				_nodes.Add(pathTransform);
			}
		}

		for (int i = 0; i < _nodes.Count; i++)
		{
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


	// increase the congestion counter

	public void IncreaseCongestion()
	{
		_congestion++;
		NotifyCongestionChange();
	}

	
	// decrease the congestion counter
	public void DecreaseCongestion()
	{
		_congestion--;
		NotifyCongestionChange();
	}

	// send new congestion to AI
	private void NotifyCongestionChange()
	{
		Dictionary<string, object> message = new Dictionary<string, object>();
		message.Add("road", gameObject.name);
		message.Add("congestion", _congestion);
		PubNubBehaviour.Instance.PublishMessage("update-congestion", message);
	}


	// get the congestion on the road
	public int GetCongestion()
	{
		return _congestion;
	}


	// return the first waypoint in the waypoint path
	public Transform[] GetWaypoints()
	{
		Transform[] pathTransforms = GetComponentsInChildren<Transform>();
		_nodes = new List<Transform>();
		foreach (Transform pathTransform in pathTransforms)
		{
			if (pathTransform != transform)
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
			if (LeftTurn.GetComponent<WaypointPath>().GetCongestion() < CongestionThreshold)
			{
				paths.Add("left-cross", LeftTurn);
			}

			if (RightTurn.GetComponent<WaypointPath>().GetCongestion() < CongestionThreshold)
			{
				paths.Add("right-cross", RightTurn);
			}

			if (StraightOn.GetComponent<WaypointPath>().GetCongestion() < CongestionThreshold)
			{
				paths.Add("straight-cross", StraightOn);
			}
		}
		else if (LeftTurn != null && RightTurn != null && StraightOn == null) // joining road at T junction
		{
			if (LeftTurn.GetComponent<WaypointPath>().GetCongestion() < CongestionThreshold)
			{
				paths.Add("left-junction-join", LeftTurn);
			}

			if (RightTurn.GetComponent<WaypointPath>().GetCongestion() < CongestionThreshold)
			{
				paths.Add("right-junction-join", RightTurn);
			}
		}
		else if (LeftTurn != null && RightTurn == null && StraightOn != null) // taking a left or going straight at T junction
		{
			if (LeftTurn.GetComponent<WaypointPath>().GetCongestion() < CongestionThreshold)
			{
				paths.Add("left-junction-leave", LeftTurn);
			}

			if (StraightOn.GetComponent<WaypointPath>().GetCongestion() < CongestionThreshold)
			{
				paths.Add("straight-junction", StraightOn);
			}
		}
		else if (LeftTurn == null && RightTurn != null && StraightOn != null
		) // taking a right or going straight at T junction
		{
			if (RightTurn.GetComponent<WaypointPath>().GetCongestion() < CongestionThreshold)
			{
				paths.Add("right-junction-crossing", RightTurn);
			}

			if (StraightOn.GetComponent<WaypointPath>().GetCongestion() < CongestionThreshold)
			{
				paths.Add("straight-junction", StraightOn);
			}
		}
		else if (LeftTurn == null && RightTurn == null && StraightOn == null)
		{
			paths.Add("despawn", null);
		}

		if (paths.Count == 0)
		{
			Dictionary<String, Transform> cantMove = new Dictionary<string, Transform>();
			cantMove.Add("cant-move", null);
			return cantMove;
		}

		String[] s = paths.Keys.ToArray();
		String choice = s[Random.Range(0, paths.Count)];
		Dictionary<String, Transform> toReturn = new Dictionary<String, Transform>();
		toReturn.Add(choice, paths[choice]);
		return toReturn;
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
		Transform[] pathTransforms = GetComponentsInChildren<Transform>();
		_nodes = new List<Transform>();

		foreach (Transform pathTransform in pathTransforms)
		{
			if (pathTransform != transform)
			{
				_nodes.Add(pathTransform);
			}
		}

		for (int i = 0; i < _nodes.Count; i++)
		{
			Vector3 currentNode = _nodes[i].position;
			Vector3 previousNode;
			if (i > 0)
			{
				previousNode = _nodes[i - 1].position;
				Gizmos.DrawLine(currentNode, previousNode);
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
				Gizmos.DrawWireSphere(currentNode, NodeSize);
			}
		}
	}

	// check if there are traffic lights attached to road
	public bool TrafficLightsOnRoad()
	{
		return TrafficLights != null;
	}
	
	
	// get the name of the turns
	public string GetLeftTurn()
	{
		return LeftTurn == null ? null : LeftTurn.gameObject.name;
	}
	
	public string GetRightTurn()
	{
		return RightTurn == null ? null : RightTurn.gameObject.name;
	}
	
	public string GetStraighOn()
	{
		return StraightOn == null ? null : StraightOn.gameObject.name;
	}

	public Vector2 GetPosition()
	{
		return new Vector2(transform.position.x, transform.position.z);
	}

}
