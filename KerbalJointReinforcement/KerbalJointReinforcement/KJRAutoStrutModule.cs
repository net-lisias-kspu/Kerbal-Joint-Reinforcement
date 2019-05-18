using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbalJointReinforcement
{
	// class for detection of autostrut cycling

	public class KJRAutoStrutModule : PartModule, IJointLockState
	{
		public static bool bIgnore = false;
		bool bUpdateRunning = false;

		private static Part BuildSensor(Vessel v, String name, Part parent)
		{
			AvailablePart partInfo = PartLoader.getPartInfoByName("KJRAutoStrutHelper");

			Part part = UnityEngine.Object.Instantiate(partInfo.partPrefab, parent.transform);
			part.gameObject.SetActive(true);

			DestroyImmediate(part.GetComponent<Collider>());
			Renderer r = part.GetComponentInChildren<Renderer>();
			if(r) r.enabled = false;

			part.name = name;
			part.persistentId = FlightGlobals.CheckPartpersistentId(part.persistentId, part, false, true);

			part.transform.position = v.rootPart.transform.position + ((v.rootPart == parent) ? (Vector3.right * 0.1f) : (Vector3.right * -0.1f));

			v.parts.Add(part);
			part.vessel = v;

			part.parent = parent;

			return part;
		}

		public static void InitializeVessel(Vessel v)
		{
#if IncludeAnalyzer
			if(!WindowManager.Instance.UseAutoStrutSensor)
				return;
#endif

			int c = v.FindPartModulesImplementing<KJRAutoStrutModule>().Count;

			if(c > 1)
				UninitializeVessel(v);

			if(c == 1)
				return;

			Part sensor1 = BuildSensor(v, "KJRsensor1", v.rootPart);
			Part sensor2 = BuildSensor(v, "KJRsensor2", sensor1);

			sensor1.AddModule("KJRAutoStrutModule");

			sensor2.autoStrutMode = Part.AutoStrutMode.Root;
			sensor2.autoStrutExcludeParent = false;
		}

		public static void UninitializeVessel(Vessel v)
		{
			if(!v || (v.parts == null))
				return;

			List<Part> toDelete = new List<Part>();

			foreach(Part p in v.parts)
			{
				if(p.partInfo.name == "KJRAutoStrutHelper")
				{
					KJRAutoStrutModule m = p.GetComponent<KJRAutoStrutModule>();
					if(m)
						UnityEngine.Object.Destroy(m);

					toDelete.Add(p);
				}
			}

			foreach(Part p in toDelete)
			{
				v.parts.Remove(p);
	
				UnityEngine.Object.Destroy(p.gameObject);
				for(int i = 0; i < p.Modules.Count; i++)
					UnityEngine.Object.Destroy(p.Modules[i]);
			}
		}

		////////////////////////////////////////
		// IJointLockState (AutoStrut support)

		private IEnumerator coroutine = null;

		bool IJointLockState.IsJointUnlocked()
		{
			if(!bIgnore && !bUpdateRunning)
			{
				if(coroutine != null)
					StopCoroutine(coroutine);

				bUpdateRunning = true;

				coroutine = DoUpdate();
				StartCoroutine(coroutine);
			}

			return true;
		}

		public IEnumerator DoUpdate()
		{
			yield return new WaitForFixedUpdate();
			bUpdateRunning = false;
			KJRManager.Instance.CycleAllAutoStrut(vessel);
		}
	}
}
