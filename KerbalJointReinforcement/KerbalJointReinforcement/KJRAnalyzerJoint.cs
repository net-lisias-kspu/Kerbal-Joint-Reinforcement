using System;

using UnityEngine;

namespace KerbalJointReinforcement
{
#if IncludeAnalyzer

	class KJRAnalyzerJoint : PartModule
	{
		public Vessel relVessel = null;

		public Vector3 relPos = Vector3.zero;
		public Quaternion relRot = Quaternion.identity;

		public int badPoint = 0;
		public int badPointFrames = 0;


		public static void RunVesselJointUpdateFunction(Vessel v)
		{
			if(!WindowManager.Instance.ShowInstability)
			{
				foreach(Part p in v.Parts)
					UninitializePart(p);

				return;
			}

			foreach(Part p in v.Parts)
				InitializePart(p);
		}

		private static void UninitializePart(Part part)
		{
			KJRAnalyzerJoint module = part.GetComponent<KJRAnalyzerJoint>();

			if(module != null)
				Destroy(module);
		}

		private static void InitializePart(Part part)
		{
			if(part.parent == null)
				return;

			KJRAnalyzerJoint module = part.GetComponent<KJRAnalyzerJoint>();

			if(!module || (module.relVessel != part.vessel))
			{
				if(!module)
				{
					module = part.gameObject.AddComponent<KJRAnalyzerJoint>();
					module.u = new MaterialColorUpdater(part.transform, PhysicsGlobals.TemperaturePropertyID, part);
				}

				module.relVessel = part.vessel;

				module.relPos = Quaternion.Inverse(part.orgRot) * (part.parent.orgPos - part.orgPos);
				module.relRot = Quaternion.Inverse(part.parent.orgRot) * part.orgRot;
			}
		}

		MaterialColorUpdater u;
		int oldVal = 0;
		int val = 0;

		int[] valH = new int[16];
		int valHidx = 0;

		public void Update()
		{
			if(val < 2)
			{
				if(oldVal >= 2)
					u.Update(Color.clear, true);
			}
			else if(val < 5)
			{
				if((oldVal < 2) || (oldVal >= 5))
					u.Update(new Color(Color.green.r, Color.green.g, Color.green.b, 0.2f), true);
			}
			else
			{
				if(oldVal != val)
					u.Update(new Color(Color.magenta.r, Color.magenta.g, Color.magenta.b, (val - 4) * 0.2f), true);
			}

			oldVal = val;

			return;
		}

		public void FixedUpdate()
		{
			if(part.parent == null)
				return;

			Vector3 curPos = part.transform.InverseTransformVector(part.parent.transform.position - part.transform.position);
			Quaternion curRot = Quaternion.Inverse(part.parent.transform.rotation) * part.transform.rotation;

			float fV = curPos.sqrMagnitude - relPos.sqrMagnitude;
			float fR = Quaternion.Angle(relRot, curRot);

			badPoint += (int)(fV / 0.002);
			badPoint += (int)(fR / 0.1);

			if(++badPointFrames > 100)
			{
				badPointFrames = 0;

				valH[valHidx++ & 0xf] = Math.Min(badPoint / 1000, 10);

				for(int i = 0; i < 16; i++)
					val = Math.Max(val, valH[i]);
			}
		}
	}

#endif
}
