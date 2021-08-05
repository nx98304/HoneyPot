using System;
using HarmonyLib;
using UnityEngine;

namespace ClassLibrary4
{
    [HarmonyPatch(typeof(EditMode), "CreateColorChangeButton",
        new Type[] {
            typeof(GameObject),
            typeof(string),
            typeof(Color),
            typeof(bool),
            typeof(Action<Color>)
        })]
    public class EditMode_CreateColorChangeButton_Patch
    {
        public static void Prefix(GameObject parent, ref bool hasAlpha)
        {      // Top          > Main > Wear                    Slot01          > Main > Accessory
            if (parent.transform.parent.parent.name == "Wear" || parent.transform.parent.parent.name == "Accessory")
            {
                hasAlpha = true;
            }
        }
    }
}