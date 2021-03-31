using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using HarmonyLib;

namespace ClassLibrary4
{
    [HarmonyPatch(typeof(AttachBoneWeight), "SetupRenderer", 
        new Type[] { typeof(Dictionary<string, Transform>), typeof(SkinnedMeshRenderer) }
    )]
    public class AttachBoneWeight_SetupRenderer_Patch
    {
        public static void Prefix(Dictionary<string, Transform> bones, ref SkinnedMeshRenderer renderer)
        {
            if(renderer.rootBone == null )
            {
                Console.WriteLine("This SkinnedMeshRenderer doesn't have a rootBone to begin with. Trying cf_J_Root...");
                renderer.rootBone = bones["cf_J_Root"];
                if(renderer.rootBone == null )
                {
                    Console.WriteLine(" -- failed. Trying cm_J_Root...");
                    renderer.rootBone = bones["cm_J_Root"];
                    if(renderer.rootBone == null )
                    {
                        Console.WriteLine(" SOMETHING IS VERY WRONG HERE!!!! ");
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(AttachBoneWeight), "SetupRenderers",
        new Type[] { typeof(Dictionary<string, Transform>), typeof(GameObject), typeof(bool) }
    )]
    public class AttachBoneWeight_SetupRenderers_Patch
    {
        private static MethodInfo AttachBoneWeight_ListupBones =
                Assembly.GetAssembly(typeof(AttachBoneWeight)).GetType("AttachBoneWeight").GetMethod("ListupBones",
                    BindingFlags.Static | BindingFlags.NonPublic);
        private static MethodInfo AttachBoneWeight_SetupRenderer =
                Assembly.GetAssembly(typeof(AttachBoneWeight)).GetType("AttachBoneWeight").GetMethod("SetupRenderer",
                    BindingFlags.Static | BindingFlags.NonPublic);

        public static bool Prefix(ref Dictionary<string, Transform> bones, GameObject attachObj, bool includeInactive)
        {
            Transform basebone_of_currentloading;
            if (bones.ContainsKey("cf_J_Root") || bones.ContainsKey("cm_J_Root"))
            {
                //bool female = bones.ContainsKey("cf_J_Root") ? true : false;
                //basebone_of_currentloading = Transform_Utility.FindTransform(attachObj.transform.parent, female ? "cf_J_Hips" : "cm_J_Hips");
                // WTF: Apparently Bonesframework's additional bones can appear under the J_Root tree AND ALSO N_O_Root tree.
                // This means we just can't assume. 
                // This should be the cloth's animator root transform, so we just ListupBones with BOTH tree. 
                basebone_of_currentloading = attachObj.transform.parent; 
                AttachBoneWeight_ListupBones.Invoke(null, new object[] { bones, basebone_of_currentloading, includeInactive });
            }

            SkinnedMeshRenderer[] renderers = attachObj.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
            foreach (SkinnedMeshRenderer r in renderers)
                AttachBoneWeight_SetupRenderer.Invoke(null, new object[] { bones, r });

            //Try to find the p_cf_anim or p_cm_anim
            Transform bone_iter = attachObj.transform.parent;
            while (bone_iter != null && bone_iter.name != "Wears")
            {
                bone_iter = bone_iter.parent;
            }

            if( bone_iter != null && bone_iter.name == "Wears" )
                bone_iter = bone_iter.parent; // Note: This should be MaleBody(Clone), FemaleBody(Clone) or named characters.
                                              //       Issue is that this might not be only used by Wears

            if ( bone_iter == null || Wears_WearInstantiate_Patches.Current_additional_rootbones_.Count == 0 )
                return false; 
            // if we found out that this is not the correct object to adjust the additional_bones
            // we just stop the procedure here. 

            Transform animator_basebone = null;
            animator_basebone = bone_iter.Find("p_cf_anim");
            if (animator_basebone == null)
            {
                animator_basebone = bone_iter.Find("p_cm_anim");
                if (animator_basebone == null)
                {
                    Console.WriteLine("VERY WRONG: Character doesn't have a animator basebone named p_cf/cm_anim?? Stop processing additional_bones.");
                    return false;
                }
            }

            // WTF: So we also don't care if it's cf_J_Root or not here...? What the hell, let's try anyway.
            Transform wears_animator_root = attachObj.transform.parent;
            Transform[] all_children_of_wears_animator_root = wears_animator_root.GetComponentsInChildren<Transform>(includeInactive);
            foreach ( Transform t in all_children_of_wears_animator_root )
            {
                if (Wears_WearInstantiate_Patches.Current_additional_rootbones_.Contains(t.name))
                {
                    Console.WriteLine("Trying to realign the local position of " + t.name);
                    Transform parent = animator_basebone.FindDescendant(t.parent.name);
                    Vector3 localPos = t.localPosition;
                    Quaternion localRot = t.localRotation;
                    Vector3 localScale = t.localScale;
                    t.SetParent(parent);
                    t.localPosition = localPos;
                    t.localRotation = localRot;
                    t.localScale = localScale;
                }
            }
            return false;
        }
    }
}