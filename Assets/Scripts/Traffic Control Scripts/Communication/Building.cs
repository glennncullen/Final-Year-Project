using System.Collections.Generic;
using DigitalRuby.PyroParticles;
using UnityEngine;

namespace Traffic_Control_Scripts.Communication
{
	public class Building : MonoBehaviour
	{

		public WaypointPath ConnectedRoad;
		private FireBaseScript[] _fires;
		public FireBaseScript Explosion;

		
		private void Awake()
		{
			_fires = GetComponentsInChildren<FireBaseScript>();
		}

		private void OnMouseDown()
		{
			if(Handler.IsSomethingOnFire) return;
			bool checkIsNull = true;
			foreach (FireBaseScript fire in _fires)
			{
				if (fire != null)
				{
					checkIsNull = false;
					break;
				}
			}
			if (checkIsNull) return;
			foreach (WaypointPath path in GameObject.Find("Roads").GetComponentsInChildren<WaypointPath>())
			{
				path.NotifyCongestionChange();
			}
			Dictionary<string, object> message = new Dictionary<string, object>();
			message.Add("start", GameObject.Find("Firebrigade").GetComponent<VehicleBehaviour>()._currentRoad.gameObject.name);
			message.Add("end", ConnectedRoad.gameObject.name);
			Handler.Instance.PublishMessage("fire-in-progress", message);
			Handler.BuildingOnFire = this;

			foreach (FireBaseScript fire in _fires)
			{
				Instantiate(Explosion, fire.transform.position, Quaternion.LookRotation(fire.transform.forward), fire.transform);
				fire.StartParticleSystems();
			}
		}


		public void ExtinguishFire()
		{
			foreach (FireBaseScript fire in _fires)
			{
				fire.Stop();
			}
		}
	}
}

