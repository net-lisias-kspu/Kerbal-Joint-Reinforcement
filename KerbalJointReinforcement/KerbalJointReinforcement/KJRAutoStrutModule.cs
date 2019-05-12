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

		private static Part partPrefab = null;

		private static void BuildPrototype()
		{
			GameObject gameObject = new GameObject("KJRAutoStrutHelper");

			partPrefab = gameObject.AddComponent<Part>();

			if((bool)FlightGlobals.fetch)
				FlightGlobals.PersistentLoadedPartIds.Remove(partPrefab.persistentId);

			partPrefab.gameObject.SetActive(false);

			AttachNode attachNode = new AttachNode("srfAttach", partPrefab.transform, 1, AttachNodeMethod.FIXED_JOINT, false, false);
			partPrefab.attachNodes.Add(attachNode);

			partPrefab.mass = 1e-6f;

			partPrefab.angularDrag = 0f;
			partPrefab.angularDragByFI = false;

			partPrefab.minimum_drag = 0f;
			partPrefab.maximum_drag = 0f;

			partPrefab.dragModel = Part.DragModel.NONE;
		}

		private static Part BuildSensor(Vessel v, String name, Part parent)
		{
			if(partPrefab == null)
				BuildPrototype();
			
			Part part = UnityEngine.Object.Instantiate(partPrefab, parent.transform);

			part.gameObject.SetActive(true);
			part.name = name;
			part.persistentId = FlightGlobals.CheckPartpersistentId(part.persistentId, part, false, true);

			part.transform.position = parent.transform.position;

			v.parts.Add(part);
			part.vessel = v;

			part.parent = parent;
			part.CreateAttachJoint(AttachModes.SRF_ATTACH);

			Rigidbody rb = part.GetComponent<Rigidbody>();
			if(!rb) rb = part.gameObject.AddComponent<Rigidbody>();

			rb.useGravity = false;
			rb.mass = 1e-6f;

			return part;
		}

		public static void InitializeVessel(Vessel v)
		{
			if(v.FindPartModuleImplementing<KJRAutoStrutModule>())
				return;

			Part sensor1 = BuildSensor(v, "KJRsensor1", v.rootPart);
			Part sensor2 = BuildSensor(v, "KJRsensor2", sensor1);

			AvailablePart availablePart = new AvailablePart();
			availablePart.name = partPrefab.name;

			sensor1.partInfo = availablePart;
			sensor2.partInfo = availablePart;

			sensor1.AddModule("KJRAutoStrutModule");

			sensor2.autoStrutMode = Part.AutoStrutMode.Root;
			sensor2.autoStrutExcludeParent = false;
		//	sensor2.CycleAutoStrut();
		}

		public static void UninitializeVessel(Vessel v)
		{
			List<Part> toDelete = new List<Part>();

			foreach(Part p in v.parts)
			{
				if(p.partInfo.name == "KJRAutoStrutHelper")
				{
					KJRAutoStrutModule m = p.GetComponent<KJRAutoStrutModule>();
					if(m)
						Destroy(m);

					toDelete.Add(p);
				}
			}

			foreach(Part p in toDelete)
			{
				p.attachJoint.DestroyJoint();
				v.parts.Remove(p);
				Destroy(p);
			}
		}

		////////////////////////////////////////
		// IJointLockState (AutoStrut support)

		bool IJointLockState.IsJointUnlocked()
		{
			if(!bIgnore)
			{
				if(bUpdateRunning)
					StopCoroutine(DoUpdate());

				bUpdateRunning = true;
				StartCoroutine(DoUpdate());
			}

			return true;
		}

		public IEnumerator DoUpdate()
		{
			yield return new WaitForFixedUpdate();
			KJRManager.Instance.CycleAllAutoStrut(vessel);
			bUpdateRunning = false;
		}
	}
}
