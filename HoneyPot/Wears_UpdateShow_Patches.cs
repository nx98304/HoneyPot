using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using Character;

namespace ClassLibrary4
{
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