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
        public static bool allow_alpha = false;
        public static void Prefix(GameObject parent, ref bool hasAlpha)
        {
            if (allow_alpha && is_part_of_wear_or_acce(parent) )
            {
                hasAlpha = true;
            }
        }

        private static bool is_part_of_wear_or_acce(GameObject o)
        {
            if (o.name == "Tops"  || o.name == "Bottoms" || o.name == "Bra"   || o.name == "Shorts" ||
                o.name == "Glove" || o.name == "Panst"   || o.name == "Socks" || o.name == "Shoes"  ||
                o.name.IndexOf("Swim") >= 0 || o.name.IndexOf("Slot") >= 0)
                return true;
            return false;
        }
    }
}