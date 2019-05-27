using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbalJointReinforcement
{
	// class to explicity exclude a part from handling by KJR (and AutoStruts)
	
	/*
	 * to use it add this into your cfg file of the part
	 * 
	 * MODULE
	 * {
	 *     name = KJRExcluded
	 * }
	 */

	public class KJRExcluded : PartModule, KJR.IKJRJoint, IJointLockState
	{
		////////////////////////////////////////
		// KJR.IKJRJoint (KJR Next support)

		bool KJR.IKJRJoint.IsJointUnlocked()
		{
			return true;
		}

		////////////////////////////////////////
		// IJointLockState (AutoStrut support)

		bool IJointLockState.IsJointUnlocked()
		{
			return true;
		}
	}
}
