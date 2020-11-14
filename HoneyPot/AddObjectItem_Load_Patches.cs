using System;
using HarmonyLib;
using Studio;

namespace ClassLibrary4
{
	// Token: 0x02000002 RID: 2
	[HarmonyPatch(typeof(AddObjectItem), "Load", new Type[]
	{
		typeof(OIItemInfo),
		typeof(ObjectCtrlInfo),
		typeof(TreeNodeObject),
		typeof(bool),
		typeof(int)
	})]
	public class AddObjectItem_Load_Patches
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		public static bool Prepare()
		{
			return true;
		}

		// Token: 0x06000002 RID: 2 RVA: 0x000022B4 File Offset: 0x000004B4
		public static void Postfix(OCIItem __result, OIItemInfo _info, ObjectCtrlInfo _parent, TreeNodeObject _parentNode, bool _addInfo, int _initialPosition)
		{
			try
			{
				if (__result != null && !(__result.itemFKCtrl != null) && !(__result.objectItem == null) && __result.objectItem.transform.childCount > 0)
				{
					__result.itemFKCtrl = __result.objectItem.AddComponent<ItemFKCtrl>();
					__result.itemFKCtrl.InitBone(__result, null, _addInfo);
					__result.dynamicBones = __result.objectItem.GetComponentsInChildren<DynamicBone>(true);
				}
			}
			catch (Exception)
			{
			}
		}
	}
}
