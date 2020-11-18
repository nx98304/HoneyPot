using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace ClassLibrary4
{
    [HarmonyPatch(typeof(WearObj), "SetupMaterials", new Type[] { typeof(WearData) })]
    public class WearObj_SetupMaterials_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            int startIndex = -1;
            int endIndex = codes.Count - 1;
            for (int i = endIndex; i >= 0; i--)
            {
                if ( codes[i].opcode == OpCodes.Br )
                {
                    startIndex = i + 1;
                    break;
                }
            }
            if (startIndex > -1)
            {
                // we cannot remove the first code of our range since some jump actually jumps to
                // it, so we replace it with a no-op instead of fixing that jump (easier).
                codes[startIndex].opcode = OpCodes.Nop;
                codes.RemoveRange(startIndex + 1, endIndex - startIndex - 1);
                // label_m br  label_n 
                // label_x nop
                // ... (we are only trying to remove these) ... 
                // label_n ret
            }
            return codes.AsEnumerable();
        }
    }
}