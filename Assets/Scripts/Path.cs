using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour {

	public Color LineColour;

	private List<Transform> Nodes = new List<Transform>();


	private void OnDrawGizmosSelected(){
		Gizmos.color = LineColour;
		Transform[] pathTransforms = GetComponentsInChildren<Transform> ();
		Nodes = new List<Transform>();

		foreach(Transform pathTransform in pathTransforms){
			if(pathTransform != transform){
					Nodes.Add (pathTransform);
			}
		}

		for(int i = 0; i < Nodes.Count; i++){
			Vector3 currentNode = Nodes [i].position;
			Vector3 previousNode;
			if (i > 0) {
				previousNode = Nodes [i - 1].position;
				Gizmos.DrawLine (currentNode, previousNode);
			} else if (i == 0 && Nodes.Count > 1) {
				previousNode = Nodes [Nodes.Count - 1].position;
				Gizmos.DrawLine (currentNode, previousNode);
			}
			Gizmos.DrawWireSphere (currentNode, 0.3f);
		}
	}
}
