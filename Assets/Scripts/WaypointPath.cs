using System.Collections.Generic;
using UnityEngine;

public class WaypointPath : MonoBehaviour {

	public Color LineColour;
	public Transform FirstConnectedRoad;
	public Transform SecondConnectedRoad;
	public Transform ThirdConnectedRoad;

	private List<Transform> _nodes = new List<Transform>();


	private void OnDrawGizmosSelected(){
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
//			else if (i == 0 && _nodes.Count > 1) {
//				previousNode = _nodes [_nodes.Count - 1].position;
//				Gizmos.DrawLine (currentNode, previousNode);
//			}
			if (i == 0)
			{
				Gizmos.DrawSphere(currentNode, 0.2f);
			}
			else
			{
				Gizmos.DrawWireSphere (currentNode, 0.1f);
			}
		}
	}

	public void DrawLines()
	{
		OnDrawGizmosSelected();
	}
	
}
