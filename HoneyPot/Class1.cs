﻿using System;
using System.Reflection;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ClassLibrary4
{
    [BepInPlugin("BepInEx.Honeypot", "HoneyPot", "1.5.0.0")]
    public class HoneyPotWrapper : BaseUnityPlugin
    {
        void Awake()
        {
            harmony = new Harmony("BepInEx.Honeypot");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            harmony.PatchAll(typeof(HoneyPot));

            doConfig();
        }

        private void doConfig()
        {
            forceColor        = Config.Bind<bool>("HoneyPot", "ForceColor", false, "[Relevant items reloading required] Enable to force-colorable to all clothings (not limited to HS1 ones); bras, shorts and accessories are force-colorable regardless. \n\nThis used to cause non-colorable clothing previously saved in cards & in StudioClothEditor to become pure white upon loading -- This doesn't happen anymore.\n\nHowever, due to how HoneyPot works, it's possible that some left over color parameters would be in your card data that is not currently used -- the left over colors will show up when you activate ForceColor when applicable.");
            entryDuplicate    = Config.Bind("HoneyPot", "Duplicate clothing entries (Restart Required)", HoneyPot.CLOTHING_ENTRY_DUPLICATE.NONE, "[Game restart required] Enable to duplicate clothing entries into other entries for more flexible clothing setup. Please understand that the duplicated entries do need to occupy difference ID ranges, and excess entries in a category may slow down opening of such category's sub-menu.");
            pbrspAlphaBlend_1 = Config.Bind<bool>("HoneyPot", "Hair: PBRsp_alpha_blend to HSStandard+ALPHABLEND", true, "[Relevant items reloading required] The ported HSStandard when set to alpha blend mode, does seem to serve as good enough PBRsp_alpha_blend substitude.\n\nDo note that the built in PH color options in the hair category doesn't support HSStandard well, you will need Material Editor. Also if prior to activate this option your card is already saved with Material Editor data, Material Editor probably will take higher priority.");
            hairHSStandard    = Config.Bind<bool>("HoneyPot", "Hair: Keep HSStandard on HS1 hairs", true, "[Relevant items reloading required] PH hair shaders are generally better than HS1 shaders even for HS1 hairs, but some hairs that were using HSStandard will NEVER work with PH hair shaders without render queue issue or weird highlights. Use this option with your own judgement.\n\nDo note that the built in PH color options in the hair category doesn't support HSStandard well, you will need Material Editor. Also if prior to activate this option your card is already saved with Material Editor data, Material Editor probably will take higher priority.");

            forceColor.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.force_color_everything_that_doesnt_have_materialcustoms = forceColor.Value;
            };

            entryDuplicate.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.entry_duplicate_flags = entryDuplicate.Value;
            };

            pbrspAlphaBlend_1.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.PBRsp_alpha_blend_to_hsstandard = pbrspAlphaBlend_1.Value;
            };

            hairHSStandard.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.hsstandard_on_hair = hairHSStandard.Value;
            };

            HoneyPot.force_color_everything_that_doesnt_have_materialcustoms = forceColor.Value;
            HoneyPot.entry_duplicate_flags                                   = entryDuplicate.Value;
            HoneyPot.PBRsp_alpha_blend_to_hsstandard                         = pbrspAlphaBlend_1.Value;
            HoneyPot.hsstandard_on_hair                                      = hairHSStandard.Value;
        }

        public void OnLevelWasLoaded(int level)
        {
            Console.WriteLine("HoneyPot - OnLevelWasLoaded: Level " + level);
            if (GameObject.Find("HoneyPot") != null) return;

            // Note: The lobby scene, chara maker and H scene all have different level value that is > 0
            //       however, studio startup already is level 0 and without the safe guard HoneyPot gets duplicated.
            //       and the Studio fully loaded scene is level 1
            //       If it is studio, we actually don't want to load HoneyPot too early
            //       to avoid multiple initializations and some erroneous order of init causing NREs
            if (level > 0)
            {
                Console.WriteLine("Initializing HoneyPot after level " +level+ " was loaded.");
                GameObject gameObject = new GameObject("HoneyPot");
                hp = gameObject.AddComponent<HoneyPot>();
                hp.SetHarmony(harmony);
                hp.gameObject.SetActive(true);
            }
        }

        private static ConfigEntry<bool> forceColor;
        private static ConfigEntry<HoneyPot.CLOTHING_ENTRY_DUPLICATE> entryDuplicate;
        private static ConfigEntry<bool> pbrspAlphaBlend_1;
        private static ConfigEntry<bool> hairHSStandard;

        private Harmony harmony;
        private HoneyPot hp;
    }
}
