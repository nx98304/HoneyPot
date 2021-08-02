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
                renderer.rootBone = bones.ContainsKey("cf_J_Root") ? bones["cf_J_Root"] : null;
                if(renderer.rootBone == null )
                {
                    Console.WriteLine(" -- failed. Trying cm_J_Root...");
                    renderer.rootBone = bones.ContainsKey("cm_J_Root") ? bones["cm_J_Root"] : null;
                    if(renderer.rootBone == null )
                    {
                        Console.WriteLine(" ---- failed again, SOMETHING IS VERY WRONG HERE!!!! ");
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

        // Note: This no longer relies on getting pre-defined "additional_bones" record from asset bundle, 
        //       because now this unifies both HS *AND* PH clothings that requires additional bones. 
        public static HashSet<string> Current_additional_rootbones_ = new HashSet<string>();

        private static Transform FindHumanAnimTransformFromWears(Transform from_this_child)
        {
            while (from_this_child != null && from_this_child.name != "Wears")
                from_this_child = from_this_child.parent;

            if (from_this_child != null && from_this_child.name == "Wears")
                from_this_child = from_this_child.parent;
            // Note: This should be MaleBody(Clone), FemaleBody(Clone) or named characters.
            if (from_this_child == null)
            {
                Console.WriteLine("VERY WRONG: Somehow this wear piece cannot trace back to a Human object?");
                return null;
            }
            Transform animator_basebone = from_this_child.Find("p_cf_anim");
            if (animator_basebone == null)
            {
                animator_basebone = from_this_child.Find("p_cm_anim");
                if (animator_basebone == null)
                    Console.WriteLine("VERY WRONG: Character doesn't have a animator basebone named p_cf/cm_anim?? Stop processing additional_bones.");
            }
            return animator_basebone;
        }

        private static void BFS_test(Transform human_animator_basebone, Transform wear_animator_basebone, string starting_humanbone_name, string starting_wearbone_name)
        {
            Transform iter_from_human_animator =
                Transform_Utility.FindTransform(human_animator_basebone, starting_humanbone_name);
            if (iter_from_human_animator == null) return;

            Transform iter_from_wear_animator = null;
            Queue<Transform> bone_BFS_queue = new Queue<Transform>();
            bone_BFS_queue.Enqueue(iter_from_human_animator);
            while (bone_BFS_queue.Count > 0)
            {
                iter_from_human_animator = bone_BFS_queue.Dequeue();
                if (iter_from_human_animator.name == "cf_J_Mune00_t_L")
                    iter_from_human_animator = iter_from_human_animator.FindDescendant("cf_J_Mune00_s_L");
                else if (iter_from_human_animator.name == "cf_J_Mune00_t_R")
                    iter_from_human_animator = iter_from_human_animator.FindDescendant("cf_J_Mune00_s_R");
                else if (iter_from_human_animator.name == iter_from_human_animator.parent.name ||
                         iter_from_human_animator.name == "cf_N_k" ||
                         iter_from_human_animator.name == "cm_N_k" ||
                         iter_from_human_animator.name == "N_move" ) 
                    continue;
                //    NOTE: Somehow the PH p_cf_anim bone hierarchy is different from the clothings at Mune00 and Mune00_t
                //          but they should become the same again at Mune00_s_L/R 
                // skipped: cf&cm_N_k, cf_J_Mune00>cf_J_Mune00 ; Also N_move -- I am assuming no one is stupid enough to 
                //          add additional_bones under N_move. But I might be proved wrong... 

                iter_from_wear_animator = Transform_Utility.FindTransform(wear_animator_basebone, iter_from_human_animator.name);

                if (iter_from_wear_animator != null)
                {
                    for (int i = 0; i < iter_from_wear_animator.childCount; i++)
                    {
                        Transform child = iter_from_wear_animator.GetChild(i);
                        // If we want to notice the additional bones in the clothings, that additional bone 
                        // cannot have 0 child, since it would be useless anyway. This is avoiding a lot of HS clothings
                        // that seems to have "_end" leaf bones, and PH clothings seem to not have those.
                        if ( child.childCount > 0 && iter_from_human_animator.Find(child.name) == null &&
                             child.name != iter_from_human_animator.parent.name ) // make sure we don't have child / parent inversion.
                        {    // so far the inversion we see are just cx_bone and cx_bone_s inversion, let's hope there aren't more.
                            Console.WriteLine(" -- additional bone: " + iter_from_human_animator.name + "/" + child.name);
                            Current_additional_rootbones_.Add(child.name);
                        }
                    }
                }
                for (int i = 0; i < iter_from_human_animator.childCount; i++)
                    bone_BFS_queue.Enqueue(iter_from_human_animator.GetChild(i));
            }
        }

        public static bool Prefix(ref Dictionary<string, Transform> bones, GameObject attachObj, bool includeInactive)
        {
            // TODO: Apparently BonesFramework can add bones to custom heads. 
            //       This is something that I am pretty sure this procedure don't work. 
            //       But if PH is going to support custom heads better one day, however slim possibility that is
            //       this will need to get another fix. I think it's best we separate the procedure that processes
            //       body bones and face bones. These 2 set of bones are separate in the main game code anyway. 

            SkinnedMeshRenderer[] renderers = attachObj.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
            Transform wear_animator_basebone = attachObj.transform.parent;
            Transform human_animator_basebone = null;
            bool skinned_face_acce = false;

            if( attachObj.name == "AcceParent")
            {
                skinned_face_acce = Transform_Utility.FindTransform(attachObj.transform, "cf_J_FaceRoot") != null ||
                                    Transform_Utility.FindTransform(attachObj.transform, "cm_J_FaceRoot") != null;
                //Note: This is really important here -- for Non-Head Skinned accessories, we still want to 
                //      potentially process them for additional bones, however the attachObj here isn't N_O_root, 
                //      it is already "AcceParent" which is one-level above the prefab animator. 
                //      If we don't reset wear_animator_basebone here, all the following code will break. 
                wear_animator_basebone = attachObj.transform.GetChild(0);
            }

            Current_additional_rootbones_.Clear();
            if (wear_animator_basebone.name != "cf_body_00" && wear_animator_basebone.name != "cf_body_mnpb" &&
                wear_animator_basebone.name != "cm_body_00" && wear_animator_basebone.name != "cm_body_mnpb" &&
                wear_animator_basebone.name != "N_silhouette" && skinned_face_acce == false)
            {
                // Skip when it is currently Body object that is loading and not Wears.
                // Also skip all Skinned Accessory that is attached to FaceRoot.

                human_animator_basebone = FindHumanAnimTransformFromWears(wear_animator_basebone);
                bool female = bones.ContainsKey("cf_J_Root") ? true : false;
                // WTF: Apparently Bonesframework's additional bones can appear under the J_Root tree AND ALSO N_O_Root tree.
                // This means we just can't assume. 
                // This should be the cloth's animator root transform, so we just ListupBones with BOTH tree. 
                // Also note, when this is a skinned acce, we want to limit it to AcceParent instead of Wears.
                // Otherwise AcceParent will be treated as an additional bone. 

                while (wear_animator_basebone.parent.name != "Wears" && wear_animator_basebone.parent.name != "AcceParent")
                    wear_animator_basebone = wear_animator_basebone.parent;

                //Console.WriteLine(wear_animator_basebone.name + ": -- Checking J_Root tree --");
                BFS_test(human_animator_basebone, wear_animator_basebone, female ? "cf_J_Root" : "cm_J_Root", female ? "cf_J_Root" : "cm_J_Root");
                //Console.WriteLine(wear_animator_basebone.name + ": -- Checking N_O_root tree --");
                foreach (SkinnedMeshRenderer r in renderers)
                {
                    for (int i = 0; i < r.transform.childCount; i++) {
                        string name = r.transform.GetChild(i).name;
                        BFS_test(human_animator_basebone, attachObj.transform, name, name);
                    }
                }
                // list up all bone names from the clothing animator base to make sure additional bones are among them.
                AttachBoneWeight_ListupBones.Invoke(null, new object[] { bones, wear_animator_basebone, includeInactive });
            }

            /* The original SetupRenderers */
            foreach (SkinnedMeshRenderer r in renderers)
                AttachBoneWeight_SetupRenderer.Invoke(null, new object[] { bones, r });
            /* End of the original SetupRenderers */

            if (human_animator_basebone == null || Current_additional_rootbones_.Count == 0)
                return false;
            // if we found out that this is not the correct object to adjust the additional_bones
            // we just stop the procedure here. 

            Transform[] all_children_of_wears_animator_root =
                wear_animator_basebone.GetComponentsInChildren<Transform>(includeInactive);
            foreach ( Transform t in all_children_of_wears_animator_root )
            {
                if (Current_additional_rootbones_.Contains(t.name))
                {
                    Console.WriteLine("Trying to realign the local position of " + t.name);
                    Transform parent = human_animator_basebone.FindDescendant(t.parent.name);
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