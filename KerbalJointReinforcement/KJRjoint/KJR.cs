using System;

namespace KJR
{
	public class KJR
	{
		private static Type KJRManagerType = null;
		private static System.Reflection.MethodInfo KJRManagerCycleAllAutoStrutMethod = null;


		static public void CycleAllAutoStrut(Vessel v)
		{
			if(KJRManagerCycleAllAutoStrutMethod == null)
			{
				AssemblyLoader.loadedAssemblies.TypeOperation (t => {
					if(t.FullName == "KerbalJointReinforcement.KJRManager") { KJRManagerType = t; } });

				if(KJRManagerType != null)
					KJRManagerCycleAllAutoStrutMethod = KJRManagerType.GetMethod("CycleAllAutoStrut");
			}

			if(KJRManagerCycleAllAutoStrutMethod != null)
				KJRManagerCycleAllAutoStrutMethod.Invoke(null, new object[] { v });

		//	v.CycleAllAutoStrut(); -> muss der Aufrufer manuell machen
		}
	}
}
