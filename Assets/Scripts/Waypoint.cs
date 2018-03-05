using UnityEngine;


public class Waypoint : MonoBehaviour
{

	public bool IsLastOnRoad = false;
	
	private void OnDrawGizmosSelected ()
	{
		transform.parent.GetComponent<WaypointPath>().DrawLines();
	}
	
}
