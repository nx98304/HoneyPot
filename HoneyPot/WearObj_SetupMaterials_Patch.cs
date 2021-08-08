using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Character;

namespace ClassLibrary4
{
    // Note: Before HoneyPot calls WearObj.SetupMaterials in the postfix of WearInstantiate, 
    //       the game will call it first when the WearObj is instantiated in the body of WearInstantiate. 
    //       so this makes sure we intercept the MaterialCustoms initial value from the abdata right after it is loaded. 
    //       This is needed for the reset color functionality that was added in 1.5.0.       
    //
    //       However, technically this implementation is wrong, because actually mod ID can be the same across different
    //       categories of clothes. We are not sure if modders really used unique IDs for ALL items, 
    //       or some of them actually reuses certain ranges of IDs in different categories. 
    [HarmonyPatch(typeof(WearObj), "SetupMaterials", new Type[] { typeof(WearData) })]
    public class WearObj_SetupMaterials_Prefix
    {
        public static void Prefix(WearObj __instance)
        {
            WEAR_TYPE type = __instance.type;
            int id = __instance.wearParam.GetWearID(type);
            MaterialCustoms mc = __instance.obj.GetComponent<MaterialCustoms>();
            if ( id >= 0 && !HoneyPot.orig_wear_colors.ContainsKey(id) && mc )
            {
                ColorParameter_PBR2 color = new ColorParameter_PBR2(mc);
                HoneyPot.orig_wear_colors.Add(id, color);
            }
        }
    }

    // Note: This is for removing a final instruction inside WearObj.SetupMaterials so that it doesn't out right 
    //       removes wearParam.color when loading a card and sees that particular WearObj doesn't have MaterialCustoms
    //       because ALL HS1 clothings doesn't have MaterialCustoms. 
    //       However this also make it possible for unused wearParam.color to be saved to a card: 
    //       1) attach a clothing that is colorable
    //       2) and now attach a clothing that is NOT colorable -- the wearParam.color from 1) is not cleared because of this patch
    //       3) save the card -- the wearParam.color is in there now.
    //       4) this compounds with ForceColor option that these left over colors will potentially show up unwanted. 
    //       This has been more or less fixed when I implemented the better ForceColor option.

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