/*
Kerbal Joint Reinforcement /L
Copyright 2015, Michael Ferrara, aka Ferram4
Copyright 2018, LisiasT

    Developers: Michael Ferrara (aka Ferram4), Meiru, LisiasT

    This file is part of Kerbal Joint Reinforcement.

    Kerbal Joint Reinforcement is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    Kerbal Joint Reinforcement is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Kerbal Joint Reinforcement.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using IOData = KSPe.IO.Data;
using IOAsset = KSPe.IO.Asset;
using KIO = KSP.IO;
using System.Diagnostics;

namespace KerbalJointReinforcement
{
    public static class KJRJointUtils
    {

        public static bool reinforceAttachNodes = false;
        public static bool multiPartAttachNodeReinforcement = true;
        public static bool reinforceDecouplersFurther = false;
        public static bool reinforceLaunchClampsFurther = false;
        public static bool useVolumeNotArea = false;

        public static float angularDriveSpring = 0;
        public static float angularDriveDamper = 0;
        public static float angularMaxForceFactor = 0;

        public static float breakForceMultiplier = 1;
        public static float breakTorqueMultiplier = 1;

        public static float breakStrengthPerArea = 40;
        public static float breakTorquePerMOI = 40000;
        public static float surfaceAttachAreaMult = 10;
        public static float surfaceAttachMOIMult = 10;

        public static float decouplerAndClampJointStrength = float.PositiveInfinity;

        public static float stiffeningExtensionMassRatioThreshold = 5;

        public static List<string> exemptPartTypes = new List<string>();
        public static List<string> exemptModuleTypes = new List<string>();
        public static List<string> decouplerStiffeningExtensionType = new List<string>();

        public static float massForAdjustment = 0.001f;
        
        private static bool debug = false;

        private static readonly IOAsset.PluginConfiguration CONFIG = IOAsset.PluginConfiguration.CreateForType<KJRManager>("config.xml");
        private static readonly IOData.PluginConfiguration USER = IOData.PluginConfiguration.CreateForType<KJRManager>("user.xml");
        
		public static void LoadConstants()
		{
            ClearConstants();

			if (CONFIG.exists()) try {
				CONFIG.load();
				LoadConstants(CONFIG);
				Log.info("Configuration file loaded.");
			}
			catch (System.Exception e)
			{
				Log.err("Configuration file has an error! Plugin will not work properly due {0}!", e.Message);
			}
			else
				Log.err("Configuration file does not exist - KJR will continue with default values!");

            if (USER.exists()) try {
				USER.load();
				LoadConstants(USER);
				Log.info("User customizable file loaded.");
			}
			catch (System.Exception e)
			{
				Log.err("User customizable file has an error! Plugin will not work properly due {0}!", e.Message);
			}
			else
				Log.err("User customizable file does not exist. Only Stock values are in use.");		

			Log.debuglevel = debug ? 5 : 3;
			if (Log.debuglevel > 3)
				LoadConstants_Debug();
		}

		private static void ClearConstants()
		{
            reinforceAttachNodes = true;
			multiPartAttachNodeReinforcement = true;
			reinforceDecouplersFurther = true;
			reinforceLaunchClampsFurther = true;
			useVolumeNotArea = true;

			angularDriveSpring = 0;
			angularDriveDamper = 0;
			angularMaxForceFactor = 0;
			
			breakForceMultiplier = 1;
			breakTorqueMultiplier = 1;
			
			breakStrengthPerArea = 40;
			breakTorquePerMOI = 40000;
			
			decouplerAndClampJointStrength = float.PositiveInfinity;
			stiffeningExtensionMassRatioThreshold = 5;
			massForAdjustment = 1;

			exemptPartTypes.Clear();
            exemptModuleTypes.Clear();
            decouplerStiffeningExtensionType.Clear();
            
            debug = false;
		}
	
		private static void LoadConstants(KIO.PluginConfiguration config)
        {
			reinforceAttachNodes = config.GetValue<bool>("reinforceAttachNodes", reinforceAttachNodes);
            multiPartAttachNodeReinforcement = config.GetValue<bool>("multiPartAttachNodeReinforcement", multiPartAttachNodeReinforcement);
            reinforceDecouplersFurther = config.GetValue<bool>("reinforceDecouplersFurther", reinforceDecouplersFurther);
            reinforceLaunchClampsFurther = config.GetValue<bool>("reinforceLaunchClampsFurther", reinforceLaunchClampsFurther);
            useVolumeNotArea = config.GetValue<bool>("useVolumeNotArea", useVolumeNotArea);

            angularDriveSpring = config.GetValue<float>("angularDriveSpring", angularDriveSpring);
            angularDriveDamper = config.GetValue<float>("angularDriveDamper", angularDriveDamper);
            angularMaxForceFactor = config.GetValue<float>("angularMaxForceFactor", angularMaxForceFactor);
            if (angularMaxForceFactor < 0)
                angularMaxForceFactor = float.MaxValue;

            breakForceMultiplier = config.GetValue<float>("breakForceMultiplier", breakForceMultiplier);
            breakTorqueMultiplier = config.GetValue<float>("breakTorqueMultiplier", breakTorqueMultiplier);

            breakStrengthPerArea = config.GetValue<float>("breakStrengthPerArea", breakStrengthPerArea);
            breakTorquePerMOI = config.GetValue<float>("breakTorquePerMOI", breakTorquePerMOI);

            decouplerAndClampJointStrength = config.GetValue<float>("decouplerAndClampJointStrength", decouplerAndClampJointStrength);
            if (decouplerAndClampJointStrength < 0)
                decouplerAndClampJointStrength = float.PositiveInfinity;

            stiffeningExtensionMassRatioThreshold = config.GetValue<float>("stiffeningExtensionMassRatioThreshold", stiffeningExtensionMassRatioThreshold);

            massForAdjustment = config.GetValue<float>("massForAdjustment", massForAdjustment);

            int i = 0;
            do
            {
                string tmpPart, tmpModule, tmpDecoupler;
                tmpPart = config.GetValue("exemptPartType" + i, "");
                tmpModule = config.GetValue("exemptModuleType" + i, "");
                tmpDecoupler = config.GetValue("decouplerStiffeningExtensionType" + i, "");

                if (tmpPart == "" && tmpModule == "" && tmpDecoupler == "")
                    break;

                if (tmpPart != "")
                    exemptPartTypes.Add(tmpPart);
                if (tmpModule != "")
                    exemptModuleTypes.Add(tmpModule);
                if (tmpDecoupler != "")
                    decouplerStiffeningExtensionType.Add(tmpDecoupler);

                i++;
            } while (true);

			debug = config.GetValue<bool>("debug", debug);			
        }
	
		private static void LoadConstants_Debug()
		{
            StringBuilder debugString = new StringBuilder();
            debugString.AppendLine("KJR Consntants in use:");

            debugString.AppendLine("\tAngular Drive:");
            debugString.AppendLine("\t\tSpring: " + angularDriveSpring);
            debugString.AppendLine("\t\tDamp: " + angularDriveDamper);
            debugString.AppendLine("\t\tMax Force Factor: " + angularMaxForceFactor);

            debugString.AppendLine("");
            debugString.AppendLine("\tJoint Strength Multipliers:");
            debugString.AppendLine("\t\tForce Multiplier: " + breakForceMultiplier);
            debugString.AppendLine("\t\tTorque Multiplier: " + breakTorqueMultiplier);
            debugString.AppendLine("\t\tJoint Force Strength");
            debugString.AppendLine("\t\t\tPer Unit Area: " + breakStrengthPerArea);
            debugString.AppendLine("\t\t\tPer Unit MOI: " + breakTorquePerMOI);

            debugString.AppendLine("");
            debugString.AppendLine("\tStrength For Additional Decoupler And Clamp Joints: " + decouplerAndClampJointStrength);

            debugString.AppendLine("");
            debugString.AppendLine("\tDebug Output: " + (Log.debuglevel > 3));
            debugString.AppendLine("\tReinforce Attach Nodes: " + reinforceAttachNodes);
            debugString.AppendLine("\tReinforce Decouplers Further: " + reinforceDecouplersFurther);
            debugString.AppendLine("\tReinforce Launch Clamps Further: " + reinforceLaunchClampsFurther);
            debugString.AppendLine("\tUse Volume For Calculations, Not Area: " + useVolumeNotArea);
            debugString.AppendLine("\tMass For Joint Adjustment: " + massForAdjustment);

            debugString.AppendLine("");
            debugString.AppendLine("\tExempt Part Types");
            foreach (string s in exemptPartTypes)
                debugString.AppendLine("\t\t"+s);

            debugString.AppendLine("");
            debugString.AppendLine("\tExempt Module Types");
            foreach (string s in exemptModuleTypes)
                debugString.AppendLine("\t\t"+s);

            debugString.AppendLine("");
            debugString.AppendLine("\tDecoupler Stiffening Extension Types");
            foreach (string s in decouplerStiffeningExtensionType)
                debugString.AppendLine("\t\t"+s);

            debugString.AppendLine("");
            debugString.AppendLine("\tDecoupler Stiffening Extension Mass Ratio Threshold: " + stiffeningExtensionMassRatioThreshold);

            Log.detail(debugString.ToString());
		}

		////////////////////////////////////////
		// find part information

		public static float MaximumPossiblePartMass(Part p)
        {
            float maxMass = p.mass;
            foreach (PartResource r in p.Resources)
                maxMass += (float)(r.info.density * r.maxAmount);

            Log.dbg("Maximum mass for part {0} is {1}", p.partInfo.title, maxMass);
            return maxMass;
        }

        public static Vector3 GuessUpVector(Part part)
        {
            // For intakes, use the intake vector
            if (part.Modules.Contains<ModuleResourceIntake>())
            {
                ModuleResourceIntake i = part.Modules.GetModule<ModuleResourceIntake>();
                Transform intakeTrans = part.FindModelTransform(i.intakeTransformName);
                return part.transform.InverseTransformDirection(intakeTrans.forward);
            }
            // If surface attachable, and node normal is up, check stack nodes or use forward
            else if (part.srfAttachNode != null &&
                     part.attachRules.srfAttach &&
                     Mathf.Abs(part.srfAttachNode.orientation.normalized.y) > 0.9f)
            {
                // When the node normal is exactly Vector3.up, the editor orients forward along the craft axis
                Vector3 dir = Vector3.forward;
                bool first = true;

                foreach (AttachNode node in part.attachNodes)
                {
                    // Doesn't seem to ever happen, but anyway
                    if (node.nodeType == AttachNode.NodeType.Surface)
                        continue;

                    // If all node orientations agree, use that axis
                    if (first)
                    {
                        first = false;
                        dir = node.orientation.normalized;
                    }
                    // Conflicting node directions - bail out
                    else if (Mathf.Abs(Vector3.Dot(dir, node.orientation.normalized)) < 0.9f)
                        return Vector3.up;
                }

                Log.dbg("{0}: Choosing axis {1} for KJR surface attach {2}.", part.partInfo.title, dir, (first ? "" : " from node"));

                return dir;
            }
            else
            {
                return Vector3.up;
            }
        }

        public static Vector3 CalculateExtents(Part p, Vector3 up)
        {
            up = up.normalized;

            // Align y axis of the result to the 'up' vector in local coordinate space
            if (Mathf.Abs(up.y) < 0.9f)
                return CalculateExtents(p, Quaternion.FromToRotation(Vector3.up, up));

            return CalculateExtents(p, Quaternion.identity);
        }

        public static Vector3 CalculateExtents(Part p, Vector3 up, Vector3 forward)
        {
            // Adjust forward to be orthogonal to up; LookRotation might do the opposite
            Vector3.OrthoNormalize(ref up, ref forward);

            // Align y to up and z to forward in local coordinate space
            return CalculateExtents(p, Quaternion.LookRotation(forward, up));
        }

        public static Vector3 CalculateExtents(Part p, Quaternion alignment)
        {
            Vector3 maxBounds = new Vector3(-100, -100, -100);
            Vector3 minBounds = new Vector3(100, 100, 100);

            // alignment transforms from our desired rotation to the local coordinates, so inverse needed
            Matrix4x4 rotation = Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(alignment), Vector3.one);
            Matrix4x4 base_matrix = rotation * p.transform.worldToLocalMatrix;

            foreach(Transform t in p.FindModelComponents<Transform>())         //Get the max boundaries of the part
            {
                MeshFilter mf = t.GetComponent<MeshFilter>();
                if ((mf == null) || (mf.sharedMesh == null))
                    continue;

                Matrix4x4 matrix = base_matrix * t.transform.localToWorldMatrix;

                foreach (Vector3 vertex in mf.sharedMesh.vertices)
                {
                    Vector3 v = matrix.MultiplyPoint3x4(vertex);

                    maxBounds.x = Mathf.Max(maxBounds.x, v.x);
                    minBounds.x = Mathf.Min(minBounds.x, v.x);
                    maxBounds.y = Mathf.Max(maxBounds.y, v.y);
                    minBounds.y = Mathf.Min(minBounds.y, v.y);
                    maxBounds.z = Mathf.Max(maxBounds.z, v.z);
                    minBounds.z = Mathf.Min(minBounds.z, v.z);
                }
            }

            if (maxBounds == new Vector3(-100, -100, -100) && minBounds == new Vector3(100, 100, 100))
            {
                Log.warn("extents could not be properly built for part {0}", p.partInfo.title);
                maxBounds = minBounds = Vector3.zero;
            }
            else
                Log.dbg("Extents: {0} .. {1} = {2}", minBounds, maxBounds, (maxBounds - minBounds));

            //attachNodeLoc = p.transform.worldToLocalMatrix.MultiplyVector(p.parent.transform.position - p.transform.position);
            return maxBounds - minBounds;
        }

        public static float CalculateRadius(Part p, Vector3 attachNodeLoc)
        {
            // y along attachNodeLoc; x,z orthogonal
            Vector3 maxExtents = CalculateExtents(p, attachNodeLoc);

            // Equivalent radius of an ellipse painted into the rectangle
            return Mathf.Sqrt(maxExtents.x * maxExtents.z) / 2;
        }

        public static float CalculateSideArea(Part p, Vector3 attachNodeLoc)
        {
            Vector3 maxExtents = CalculateExtents(p, attachNodeLoc);
            //maxExtents = Vector3.Exclude(maxExtents, Vector3.up);

            return maxExtents.x * maxExtents.z;
        }

        ////////////////////////////////////////
        // find joint reinforcement information

        public static bool IsJointAdjustmentAllowed(Part p)
        {
            if (p.HasFreePivot())
                return false;
            
            if (p.GetComponent<KerbalEVA>() != null)
                return false;

            foreach(string s in exemptPartTypes)
                if (p.GetType().ToString() == s)
                    return false;

            foreach(string s in exemptModuleTypes)
                if (p.Modules.Contains(s))
                    return false;

            return true;
        }

        public static bool GetsDecouplerStiffeningExtension(Part p)
        {
            string typeString = p.GetType().ToString();

            foreach(string s in decouplerStiffeningExtensionType)
                if (typeString == s)
                    return true;

            foreach(string s in decouplerStiffeningExtensionType)
                if (p.Modules.Contains(s))
                    return true;

            return false;
        }

        public static List<Part> DecouplerPartStiffeningListParents(Part p)
        {
            List<Part> tmpPartList = new List<Part>();

            // non-physical parts are skipped over by attachJoints, so do the same
            bool extend = (p.physicalSignificance == Part.PhysicalSignificance.NONE);

            if (!extend)
                extend = GetsDecouplerStiffeningExtension(p);

            List<Part> newAdditions = new List<Part>();

            if (extend)
            {
                if (p.parent && IsJointAdjustmentAllowed(p))
                    newAdditions.AddRange(DecouplerPartStiffeningListParents(p.parent));
            }
            else
            {
                float thisPartMaxMass = MaximumPossiblePartMass(p);

                if (p.parent && IsJointAdjustmentAllowed(p))
                {
                    float massRatio = MaximumPossiblePartMass(p.parent) / thisPartMaxMass;
                    //if (massRatio < 1)
                    //    massRatio = 1 / massRatio;

                    if (massRatio > stiffeningExtensionMassRatioThreshold)
                    {
                        newAdditions.Add(p.parent);
                        Log.dbg("Part {0} added to list due to mass ratio difference", p.parent.partInfo.title);
                    }
                }
            }

            if (newAdditions.Count > 0)
                tmpPartList.AddRange(newAdditions);
            else
                extend = false;

            if (!extend)
                tmpPartList.Add(p);

            return tmpPartList;
        }

        public static List<Part> DecouplerPartStiffeningListChildren(Part p)
        {
            List<Part> tmpPartList = new List<Part>();

            // non-physical parts are skipped over by attachJoints, so do the same
            bool extend = (p.physicalSignificance == Part.PhysicalSignificance.NONE);

            if (!extend)
                extend = GetsDecouplerStiffeningExtension(p);

            List<Part> newAdditions = new List<Part>();

            if (extend)
            {
                if (p.children != null)
                {
                    foreach(Part q in p.children)
                    {
                        if (q != null && q.parent == p && IsJointAdjustmentAllowed(q))
                            newAdditions.AddRange(DecouplerPartStiffeningListChildren(q));
                    }
                }
            }
            else
            {
                float thisPartMaxMass = MaximumPossiblePartMass(p);
                if (p.children != null)
                    foreach(Part q in p.children)
                    {
                        if (q != null && q.parent == p && IsJointAdjustmentAllowed(q))
                        {
                            float massRatio = MaximumPossiblePartMass(q) / thisPartMaxMass;
                            //if (massRatio < 1)
                            //    massRatio = 1 / massRatio;

                            if (massRatio > stiffeningExtensionMassRatioThreshold)
                            {
                                newAdditions.Add(q);
                                Log.dbg("Part {0} added to list due to mass ratio difference", q.partInfo.title);
                            }
                        }
                    }
            }

            if (newAdditions.Count > 0)
                tmpPartList.AddRange(newAdditions);
            else
                extend = false;

            if (!extend)
                tmpPartList.Add(p);

            return tmpPartList;
        }

        ////////////////////////////////////////
        // functions

        public static ConfigurableJoint BuildJoint(Part p, Part linkPart)
        {
            ConfigurableJoint newJoint;
            
            if ((p.mass >= linkPart.mass) || (p.rb == null))
            {
                newJoint = p.gameObject.AddComponent<ConfigurableJoint>();
                newJoint.connectedBody = linkPart.Rigidbody;
            }
            else
            {
                newJoint = linkPart.gameObject.AddComponent<ConfigurableJoint>();
                newJoint.connectedBody = p.Rigidbody;
            }

            newJoint.anchor = Vector3.zero;
            newJoint.axis = Vector3.right;
            newJoint.secondaryAxis = Vector3.forward;
            newJoint.breakForce = KJRJointUtils.decouplerAndClampJointStrength;
            newJoint.breakTorque = KJRJointUtils.decouplerAndClampJointStrength;

            newJoint.xMotion = newJoint.yMotion = newJoint.zMotion = ConfigurableJointMotion.Locked;
            newJoint.angularXMotion = newJoint.angularYMotion = newJoint.angularZMotion = ConfigurableJointMotion.Locked;

            return newJoint;
        }

        public static void AddDecouplerJointReinforcementModule(Part p)
        {
            p.AddModule("KJRDecouplerReinforcementModule");
            (p.Modules["KJRDecouplerReinforcementModule"] as KJRDecouplerReinforcementModule).OnPartUnpack();
            Log.dbg("Added KJRDecouplerReinforcementModule to part ", p.partInfo.title);
        }

        public static void AddLaunchClampReinforcementModule(Part p)
        {
            p.AddModule("KJRLaunchClampReinforcementModule");
            (p.Modules["KJRLaunchClampReinforcementModule"] as KJRLaunchClampReinforcementModule).OnPartUnpack();
            Log.dbg("Added KJRLaunchClampReinforcementModule to part ", p.partInfo.title);
        }

        public static void ConnectLaunchClampToGround(Part clamp)
        {
            float breakForce = Mathf.Infinity;
            float breakTorque = Mathf.Infinity;
            FixedJoint newJoint;

            newJoint = clamp.gameObject.AddComponent<FixedJoint>();

            newJoint.connectedBody = null;
            newJoint.anchor = Vector3.zero;
            newJoint.axis = Vector3.up;
            //newJoint.secondaryAxis = Vector3.forward;
            newJoint.breakForce = breakForce;
            newJoint.breakTorque = breakTorque;

            //newJoint.xMotion = newJoint.yMotion = newJoint.zMotion = ConfigurableJointMotion.Locked;
            //newJoint.angularXMotion = newJoint.angularYMotion = newJoint.angularZMotion = ConfigurableJointMotion.Locked;
        }
    }
}
