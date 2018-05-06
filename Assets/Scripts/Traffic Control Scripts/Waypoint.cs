using UnityEngine;


public class Waypoint : MonoBehaviour
{
	public Waypoint()
	{
		IsLastOnRoad = false;
		IsFirstOnRoad = false;
	}

	public bool IsLastOnRoad { get; set; }
	public bool IsFirstOnRoad { get; set; }

	// if this waypoint is selected, draw the whole path
	private void OnDrawGizmosSelected ()
	{
		transform.parent.GetComponent<WaypointPath>().DrawPath();
	}
}
