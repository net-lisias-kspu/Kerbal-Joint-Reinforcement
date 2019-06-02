using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace KerbalJointReinforcement
{
	// class to explicity exclude a part from handling by KJR
	
	/*
	 * to use it add this into your cfg file of the part
	 * 
	 * MODULE
	 * {
	 *     name = KJRExcluded
	 * }
	 */

	public class KJRExcluded : PartModule, KJR.IKJRJoint
	{
		public delegate bool IsJointUnlockedCallback();

		public IsJointUnlockedCallback callback = null;

		////////////////////////////////////////
		// KJR.IKJRJoint (KJR Next support)

		bool KJR.IKJRJoint.IsJointUnlocked()
		{
			if(callback != null)
				return callback();

			return true;
		}

		////////////////////////////////////////
		// IJointLockState (AutoStrut support)

	//	bool IJointLockState.IsJointUnlocked()
	//	{
	//		return true;
	//	}
	}
}
