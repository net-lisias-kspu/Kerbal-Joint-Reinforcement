using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if IncludeAutoStrutModule

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

			// remove Ferram Aerospace Research

			Component c;

			c = partInfo.partPrefab.GetComponent("GeometryPartModule");
			if(c) UnityEngine.Object.DestroyImmediate(c);

			c = partInfo.partPrefab.GetComponent("FARAeroPartModule");
			if(c) UnityEngine.Object.DestroyImmediate(c);

			c = partInfo.partPrefab.GetComponent("FARPartModule");
			if(c) UnityEngine.Object.DestroyImmediate(c);


			Part part = UnityEngine.Object.Instantiate(partInfo.partPrefab, parent.transform);
			part.gameObject.SetActive(true);

			DestroyImmediate(part.GetComponent<Collider>());
			Renderer r = part.GetComponentInChildren<Renderer>();
#if IncludeAnalyzer
			if(r) r.enabled = WindowManager.Instance.ShowAutoStrutSensor;
#else
			if(r) r.enabled = false;
#endif

			part.name = name;
		//	part.persistentId = FlightGlobals.CheckPartpersistentId(part.persistentId, part, false, true, parent.vessel.persistentId);

			part.transform.position = v.rootPart.transform.position; // + ((v.rootPart == parent) ? (v.rootPart.transform.up * 0.1f) : (v.rootPart.transform.up * -0.1f));

			v.parts.Add(part);
			part.vessel = v;

			part.parent = parent;

			return part;
		}

		public static void InitializeVessel(Vessel v) // FEHLER, klären -> wird super oft aufgerufen... evtl. zu oft???
		{
#if IncludeAnalyzer
			if(!WindowManager.Instance.UseAutoStrutSensor)
				return;
#endif
			if(v.parts.Count < 2)
				return;

			if(ReinitializeVessel(v))
				return;

			UninitializeVessel(v); // FEHLER, gleich ins ReinitializeVessel integrieren

			Part sensor1 = BuildSensor(v, "KJRsensor1", v.rootPart);
			Part sensor2 = BuildSensor(v, "KJRsensor2", sensor1);

			sensor1.AddModule("KJRAutoStrutModule");

			sensor2.autoStrutMode = Part.AutoStrutMode.Root;
			sensor2.autoStrutExcludeParent = false;
		}
		
		public static bool ReinitializeVessel(Vessel v)
		{
			if(!v || (v.parts == null))
				return false;

			int found = 0;
			List<Part> toRelocate = new List<Part>();

			foreach(Part p in v.parts)
			{
				if(p.partInfo.name == "KJRAutoStrutHelper")
				{
					++found;

					if(v.parts.IndexOf(p) + 2 < v.parts.Count)
						toRelocate.Add(p);
				}
			}

			if(toRelocate.Count > 0)
			{
				foreach(Part p in toRelocate)
					v.parts.Remove(p);

				v.parts.AddRange(toRelocate);
			}

			return found == 2;
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

#endif
