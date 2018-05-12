using System.Collections;
using System.Collections.Generic;
using Traffic_Control_Scripts.Communication;
using UnityEngine;

public class RoadComms : MonoBehaviour
{

	private WaypointPath[] _roadSegments;
	private Dictionary<string, object> _message = new Dictionary<string, object>();
	private int _counter = 0;
	
	private void Awake()
	{
		_roadSegments = GetComponentsInChildren<WaypointPath>();
		foreach (WaypointPath road in _roadSegments)
		{
			_message.Add(_counter.ToString(), new Dictionary<string, object>
			{
				{"name", road.gameObject.name},
				{"straight", road.GetStraighOn()},
				{"left", road.GetLeftTurn()},
				{"right", road.GetRightTurn()},
				{"lights", road.TrafficLightsOnRoad()},
				{"congestion", road.GetCongestion()},
				{"position", road.GetPosition()}
			});
			_counter++;
		}
		Handler.Instance.PublishMessage("all-roads", _message);
	}
}
