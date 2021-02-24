using System;
using System.Collections.Generic;
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
}