using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path : MonoBehaviour {

	public Color lineColour;

	public List<Transform> nodes = new List<Transform>();


	void OnDrawGizmosSelected(){
		Gizmos.color = lineColour;
		Transform[] pathTransforms = GetComponentsInChildren<Transform> ();
		if(nodes != null){
			nodes.Clear ();
		}

		foreach(Transform pathTransform in pathTransforms){
			if(pathTransform != transform){
					nodes.Add (pathTransform);
			}
		}

		for(int i = 0; i < nodes.Count; i++){
			Vector3 currentNode = nodes [i].position;
			Vector3 previousNode;
			if (i > 0) {
				previousNode = nodes [i - 1].position;
				Gizmos.DrawLine (currentNode, previousNode);
			} else if (i == 0 && nodes.Count > 1) {
				previousNode = nodes [nodes.Count - 1].position;
				Gizmos.DrawLine (currentNode, previousNode);
			}
			Gizmos.DrawWireSphere (currentNode, 0.3f);
		}
	}
}
