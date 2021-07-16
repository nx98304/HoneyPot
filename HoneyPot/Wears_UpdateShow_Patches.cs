using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Character;

namespace ClassLibrary4
{
    //Note: This patch is for removing the cm/cf_N_O_root transform check within WearInstantiate
    //      Because I have to make this work with HS1 clothings that doesn't conform to the HS1/PH shared clothing spec
    //      Two issues: 
    //      1. Some HS1 clothing mods doesn't respect N_O_root structure. Need to add the node into the structure. 
    //      2. Some HS1 clothing mods have erroneously have multiple N_O_roots because they use messed-up mod templates.
    //         This is subtle but PH's Transform_Utility.FindTransform uses DFS (Transform.Enumerator.MoveNext) 
    //         But we actually needs BFS here, again. 
    [HarmonyPatch(typeof(Wears), "WearInstantiate")]
    public class Wears_WearInstantiate_Patch
    {
        private static void create_N_O_root_if_not_found(ref Transform iter, Transform prefab_anim_root)
        {
            if (iter != null) return; //means N_O_root is already found prior to this, skip it. 
            bool female = Transform_Utility.FindTransform(prefab_anim_root, "cf_J_Root") != null ? true : false;
            Transform iter_from_anim_root = null;
            Transform parent_to_N_O_root = null;
            List<Transform> children_to_N_O_root = new List<Transform>();
            Queue<Transform> transform_BFS_queue = new Queue<Transform>();
            for (int i = 0; i < prefab_anim_root.childCount; i++)
                transform_BFS_queue.Enqueue(prefab_anim_root.GetChild(i));
            while (transform_BFS_queue.Count > 0)
            {
                iter_from_anim_root = transform_BFS_queue.Dequeue();
                if (iter_from_anim_root.name.IndexOf("N_") == 0)
                {
                    GameObject new_N_O_root = new GameObject( female ? "cf_N_O_root" : "cm_N_O_root" );
                    parent_to_N_O_root = iter_from_anim_root.parent;
                    
                    for (int i = 0; i < parent_to_N_O_root.childCount; i++)
                        children_to_N_O_root.Add(parent_to_N_O_root.GetChild(i));

                    for (int i = 0; i < children_to_N_O_root.Count; i++)
                        children_to_N_O_root[i].SetParent(new_N_O_root.transform);

                    new_N_O_root.transform.SetParent(parent_to_N_O_root);
                    iter = new_N_O_root.transform;
                    break;
                }
                for (int i = 0; i < iter_from_anim_root.childCount; i++)
                    transform_BFS_queue.Enqueue(iter_from_anim_root.GetChild(i));
            }
        }

        //To substitute Transform_Utility.FindTransform where needed. 
        private static Transform find_first_transform_BFS(Transform from, string name)
        {
            Queue<Transform> transform_BFS_queue = new Queue<Transform>();
            Transform result = null;
            transform_BFS_queue.Enqueue(from);
            while (transform_BFS_queue.Count > 0)
            {
                from = transform_BFS_queue.Dequeue();
                if (from.name == name)
                { //Note: we don't use result as the iterator because it must not return anything when not found. 
                    result = from;
                    break;
                }
                for (int i = 0; i < from.childCount; i++)
                    transform_BFS_queue.Enqueue(from.GetChild(i));
            }
            return result;
        }

        //Note: this whole section won't make any sense unless you are reading WearInstantiate's IL code as reference.
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo fi1  = typeof(Wears).GetField("baseBoneRoot", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo mi1 = typeof(AttachBoneWeight).GetMethod("Attach", BindingFlags.Public | BindingFlags.Static);
            MethodInfo mi2 = typeof(Wears).GetMethod("ReAttachDynamicBone"); // ??? I can't specify BindingFlags here???
            MethodInfo mi3 = typeof(Transform_Utility).GetMethod("FindTransform", BindingFlags.Public | BindingFlags.Static);
            var codes = new List<CodeInstruction>(instructions);
            int firstFindTransform_index = -1;
            int meshRootConditionStart_index = -1;
            int meshRootConditionEnd_index = -1;
            int elseBlockStart_index = -1;
            int elseBlockEnd_index = -1;
            for (int i = 1; i < codes.Count - 3; i++)
            {
                if(firstFindTransform_index < 0 &&
                   codes[i].opcode  == OpCodes.Call &&
                   codes[i].operand == mi3)
                {
                    firstFindTransform_index = i;
                    codes[i].operand = typeof(Wears_WearInstantiate_Patch).GetMethod(nameof(find_first_transform_BFS), BindingFlags.NonPublic | BindingFlags.Static);
                }
                else if(meshRootConditionStart_index < 0 &&
                        codes[i].opcode == OpCodes.Ldloc_3)
                {
                    meshRootConditionStart_index = i;
                }
                else if(meshRootConditionEnd_index < 0 &&
                        codes[i].opcode == OpCodes.Brfalse &&
                        codes[i+2].opcode == OpCodes.Ldfld && 
                        codes[i+2].operand == fi1)
                {
                    meshRootConditionEnd_index = i;
                }
                else if(elseBlockStart_index < 0 && 
                        codes[i].opcode == OpCodes.Br && 
                        codes[i-1].operand == mi1 )
                {
                    elseBlockStart_index = i;
                }
                else if(elseBlockEnd_index < 0 &&
                        codes[i].opcode == OpCodes.Call && 
                        codes[i+3].operand == mi2)
                {
                    elseBlockEnd_index = i;
                }
            }
            for (int i = meshRootConditionStart_index; i <= meshRootConditionEnd_index; i++)
                codes[i].opcode = OpCodes.Nop;
            for (int i = elseBlockStart_index; i <= elseBlockEnd_index; i++)
                codes[i].opcode = OpCodes.Nop;
            //Note: So I don't know why -- removing the instructions by deleting them will break the program
            //      while Nop works. Apparently I must have deleted some Br jump label targets with it
            //      but I can't figure out what or where when looking at the IL code. Anyhow, Nop works,
            //      that's what counts.

            //Note: What I want to do here instead of the original code: 
            //      Transform transform = Transform_Utility.FindTransform(gameObject.transform, this.wear_meshRootName);
            //      create_N_O_root_if_not_found((ref)transform, gameObject.transform);
            //      AttachBoneWeight.Attach(this.baseBoneRoot.gameObject, transform.gameObject, true);
            codes[meshRootConditionStart_index]   = new CodeInstruction(OpCodes.Ldloca, 3); //loc3 is local var transform
            codes[meshRootConditionStart_index+1] = new CodeInstruction(OpCodes.Ldloc_2);   //loc2 is local var gameObject
            codes[meshRootConditionStart_index+2] = new CodeInstruction(OpCodes.Callvirt, 
                typeof(GameObject).GetMethod("get_transform")); //gameObject.transform
            codes[meshRootConditionStart_index+3] = new CodeInstruction(OpCodes.Call, 
                typeof(Wears_WearInstantiate_Patch).GetMethod(nameof(create_N_O_root_if_not_found), BindingFlags.NonPublic | BindingFlags.Static));
            return codes.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(Wears), "ReAttachDynamicBone", 
        new Type[] { typeof(GameObject) }
    )]
    public class Wears_ReAttachDynamicBone_Patches
    {
        private static FieldInfo baseBoneRootField = typeof(Wears).GetField("baseBoneRoot", BindingFlags.Instance | BindingFlags.NonPublic);

        public static bool Prefix(Wears __instance, GameObject obj)
        {
            Transform this_baseboneroot_transform = baseBoneRootField.GetValue(__instance) as Transform;
            DynamicBone[] componentsInChildren = obj.GetComponentsInChildren<DynamicBone>(true);
            DynamicBoneCollider[] componentsInChildren2 = this_baseboneroot_transform.GetComponentsInChildren<DynamicBoneCollider>(true);
            foreach (DynamicBone dynamicBone in componentsInChildren)
            {
                if ( dynamicBone.m_Root == null ) continue;
                Transform transform = Transform_Utility.FindTransform(this_baseboneroot_transform, dynamicBone.m_Root.name);
                if (transform != null)
                {
                    dynamicBone.m_Root = transform;
                }
                else
                {
                    Transform try_the_wear_rootbone = obj.transform.Find("cf_J_Root");
                    transform = Transform_Utility.FindTransform(try_the_wear_rootbone, dynamicBone.m_Root.name);
                    if( transform != null )
                    {
                        dynamicBone.m_Root = transform;
                    }
                    else
                    {
                        try_the_wear_rootbone = obj.transform.Find("cm_J_Root");
                        transform = Transform_Utility.FindTransform(try_the_wear_rootbone, dynamicBone.m_Root.name);
                        if( transform != null )
                        {
                            dynamicBone.m_Root = transform;
                        }
                        else Debug.LogError("ダイナミックボーン付け替えに失敗:" + dynamicBone.m_Root.name);
                    }
                }
                dynamicBone.m_Colliders.Clear();
                foreach (DynamicBoneCollider item in componentsInChildren2)
                {
                    dynamicBone.m_Colliders.Add(item);
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Wears), "UpdateShow",
        new Type[] { typeof(WEAR_SHOW_TYPE) }
    )]
    public class Wears_UpdateShow_Patches
    {
        public static bool Prefix(Wears __instance, WEAR_SHOW_TYPE showType)
        {   
            WEAR_TYPE wear_TYPE = Wears.ShowToWearType[(int)showType];
            int num = (int)wear_TYPE;
            FieldInfo wearObjsField = typeof(Wears).GetField("wearObjs", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo wearParamField = typeof(Wears).GetField("wearParam", BindingFlags.Instance | BindingFlags.NonPublic);
            WearObj[] wearObjs = wearObjsField.GetValue(__instance) as WearObj[];
            WearParameter wearParam = wearParamField.GetValue(__instance) as WearParameter;
            if (wearObjs[num] == null)
            {
                return false;
            }
            GameObject obj = wearObjs[num].obj;
            WEAR_SHOW show = __instance.GetShow(showType, true);
            bool flag = true;
            bool flag2 = false;
            if (wear_TYPE == WEAR_TYPE.SWIM)
            {
                WearData wearData = __instance.GetWearData(WEAR_TYPE.SWIM);
                if (wearData != null && wearData.coordinates == 0)
                {
                    flag2 = true;
                }
            }
            if (!flag2)
            {
                flag = (show != WEAR_SHOW.HIDE);
            }
            if (flag)
            {
                flag = Wears.IsEnableWear(wearParam.isSwimwear, wear_TYPE);
            }
            if (wear_TYPE == WEAR_TYPE.BRA && flag)
            {
                if (wearObjs[0] != null && __instance.GetShow(WEAR_SHOW_TYPE.TOPUPPER, true) == WEAR_SHOW.ALL)
                {
                    flag = false;
                }
                WearData wearData2 = __instance.GetWearData(WEAR_TYPE.TOP);
                WearData wearData3 = __instance.GetWearData(WEAR_TYPE.BOTTOM);
                if (wearData2 != null)
                {
                    if (wearData2.nip) flag = true;
                    if (wearData2.braDisable) flag = false;
                }
                if (wearData3 != null && wearData3.braDisable)
                {
                    flag = false;
                }
            }
            if (wear_TYPE == WEAR_TYPE.SHORTS && flag)
            {
                WearData wearData4 = __instance.GetWearData(WEAR_TYPE.TOP);
                WearData wearData5 = __instance.GetWearData(WEAR_TYPE.BOTTOM);
                if (wearData4 != null && wearData4.shortsDisable)
                {
                    flag = false;
                }
                if (wearData5 != null && wearData5.shortsDisable)
                {
                    flag = false;
                }
            }
            if (wear_TYPE == WEAR_TYPE.BOTTOM && flag)
            {
                WearData wearData6 = __instance.GetWearData(WEAR_TYPE.TOP);
                if (wearData6 != null && wearData6.coordinates == 2)
                {
                    flag = false;
                }
            }
            WearObj wearObj = wearObjs[num];
            if (Wears.IsLower(showType))
            {
                wearObj.ChangeShow_Lower(show);
            }
            else
            {
                wearObj.obj.SetActive(flag);
                if (wearObj.liquid != null && wearObj.liquid.root != null)
                {
                    wearObj.liquid.root.SetActive(flag);
                }
                wearObj.ChangeShow_Upper(show);
            }
            return false;
        }
    }
}