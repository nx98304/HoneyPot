using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Studio;

namespace ClassLibrary4
{
	// Token: 0x0200000B RID: 11
	//[HarmonyPatch(typeof(ItemFKCtrl), "LateUpdate", null)]
	//public class ItemFKCtrl_LateUpdate_Patches
	//{
	//	// Token: 0x06000057 RID: 87 RVA: 0x00002050 File Offset: 0x00000250
	//	public static bool Prepare()
	//	{
	//		return true;
	//	}

	//	// Token: 0x06000058 RID: 88 RVA: 0x00002279 File Offset: 0x00000479
	//	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	//	{
	//		yield return new CodeInstruction(OpCodes.Ret, null);
	//		yield break;
	//	}
	//}
}
