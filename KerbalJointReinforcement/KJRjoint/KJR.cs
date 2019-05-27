using System;

namespace KJR
{
	public class KJR
	{
		private static Type KJRManagerType = null;
		private static System.Reflection.MethodInfo KJRManagerCycleAllAutoStrutMethod = null;

		private static object KJRManager = null;


		static public void CycleAllAutoStrut(Vessel v)
		{
			if(KJRManager == null)
			{
				AssemblyLoader.loadedAssemblies.TypeOperation (t => {
					if(t.FullName == "KerbalJointReinforcement.KJRManager") { KJRManagerType = t; } });

				if(KJRManagerType != null)
				{
					KJRManagerCycleAllAutoStrutMethod = KJRManagerType.GetMethod("CycleAllAutoStrut");

					KJRManager = FlightGlobals.FindObjectOfType(KJRManagerType);
				}
			}

			if(KJRManager != null)
				KJRManagerCycleAllAutoStrutMethod.Invoke(KJRManager, new object[] { v });

		//	v.CycleAllAutoStrut(); -> muss der Aufrufer manuell machen
		}
	}
}
