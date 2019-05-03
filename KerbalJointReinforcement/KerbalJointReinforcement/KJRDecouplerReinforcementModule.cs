using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbalJointReinforcement
{
	// This class adds extra joints between the parts connected to a decoupler to stiffen up the connection
	public class KJRDecouplerReinforcementModule : PartModule
	{
		protected List<ConfigurableJoint> joints = new List<ConfigurableJoint>();
		protected List<Part> neighbours = new List<Part>();
		private PartModule decoupler = null;
		//private bool radiallyAttached = false;

		public override void OnAwake()
		{
			base.OnAwake();

			foreach(PartModule m in part.Modules)
				if(m is ModuleDecouple)
				{
					decoupler = m;
					break;
				}
				else if(m is ModuleAnchoredDecoupler)
				{
					decoupler = m;
					break;
				}
		}

		public void OnDestroy()
		{
			if(joints.Count > 0)
			{
				GameEvents.onVesselCreate.Remove(OnVesselWasModified);
				GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
			}
		}

		public void OnPartPack()
		{
			if(joints.Count > 0)
			{
				GameEvents.onVesselCreate.Remove(OnVesselWasModified);
				GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
			}

			foreach(ConfigurableJoint j in joints)
				GameObject.Destroy(j);

			joints.Clear();
			neighbours.Clear();
		}

		public void OnPartUnpack()
		{
			if(part.parent == null || part.children.Count == 0)
				return;
			if(decoupler is ModuleDecouple && (decoupler as ModuleDecouple).isDecoupled)
				return;
			if(decoupler is ModuleAnchoredDecoupler && (decoupler as ModuleAnchoredDecoupler).isDecoupled)
				return;

			AddExtraJoints();
		}

		private void OnVesselWasModified(Vessel v)
		{
			if(part.vessel == v)
			{
				int i = 0;
				while((i < neighbours.Count) && (neighbours[i].vessel == v)) ++i;

				if(i < neighbours.Count)
				{
					if(KJRJointUtils.debug)
						Debug.Log("Decoupling part, destroying all extra joints of " + part.partInfo.title);

					BreakAllInvalidJointsAndRebuild();
				}
			}
		}

		private void AddExtraJoints()
		{
			List<Part> childParts = new List<Part>();
			List<Part> parentParts = new List<Part>();

			parentParts = KJRJointUtils.DecouplerPartStiffeningListParents(part.parent);

			foreach(Part p in part.children)
			{
				if(KJRJointUtils.IsJointAdjustmentAllowed(p))
				{
					childParts.AddRange(KJRJointUtils.DecouplerPartStiffeningListChildren(p));
					if(!childParts.Contains(p))
						childParts.Add(p);
				}
			}

			neighbours.Clear();
			neighbours.AddRange(parentParts);
			neighbours.AddRange(childParts);
			neighbours.Remove(part);

			parentParts.Add(part);

			StringBuilder debugString = null;

			if(KJRJointUtils.debug)
			{
				debugString = new StringBuilder();
				debugString.AppendLine(parentParts.Count + " parts above decoupler to be connected to " + childParts.Count + " below decoupler.");
				debugString.AppendLine("The following joints added by " + part.partInfo.title + " to increase stiffness:");
			}

			foreach(Part p in parentParts)
			{
				if(p == null || p.rb == null || p.Modules.Contains("ProceduralFairingDecoupler"))
					continue;

				foreach(Part q in childParts)
				{
					if(q == null || q.rb == null || p == q || q.Modules.Contains("ProceduralFairingDecoupler"))
						continue;

					if(p.vessel != q.vessel)
						continue;

					joints.Add(KJRJointUtils.BuildJoint(p, q));

					if(KJRJointUtils.debug)
						debugString.AppendLine(p.partInfo.title + " connected to part " + q.partInfo.title);
				}
			}

			if(joints.Count > 0)
			{
				GameEvents.onVesselCreate.Add(OnVesselWasModified);
				GameEvents.onVesselWasModified.Add(OnVesselWasModified);
			}

			if(KJRJointUtils.debug)
				Debug.Log(debugString.ToString());
		}

		private void BreakAllInvalidJointsAndRebuild()
		{
			GameEvents.onVesselCreate.Remove(OnVesselWasModified);
			GameEvents.onVesselWasModified.Remove(OnVesselWasModified);

			foreach(ConfigurableJoint j in joints)
				GameObject.Destroy(j);

			joints.Clear();

			neighbours.Clear();

			if(part.parent == null || part.children.Count == 0)
				return;
			if(decoupler is ModuleDecouple && (decoupler as ModuleDecouple).isDecoupled)
				return;
			if(decoupler is ModuleAnchoredDecoupler && (decoupler as ModuleAnchoredDecoupler).isDecoupled)
				return;

			AddExtraJoints();
		}
	}
}
