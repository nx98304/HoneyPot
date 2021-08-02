using System;
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
            forceColor        = Config.Bind<bool>("HoneyPot", "ForceColor", false, "[Relevant items reloading required] Enable to force-colorable to all clothings (not limited to HS1 ones); bras, shorts and accessories are force-colorable regardless.");
            doTransport       = Config.Bind<bool>("HoneyPot", "DoTransport (Restart Required)", false, "[Game restart required] Enable to duplicate all swimsuits into bras and shorts categories, among other things...; Please understand that the duplicated swimsuits in Bra/Shorts category do need to occupy difference ID ranges, so it will somewhat raise the possibility of clothing mod ID conflict.");
            fixBadTransform   = Config.Bind<bool>("HoneyPot", "(DEBUG) fixBadTransform", false, "[Relevant items reloading required] Some HS1 hair has bad Transfrom setup, (x-rotate 90-degree, scaled by 0.01 and then by 100, etc) see if we can fix that.");
            pbrspAlphaBlend_1 = Config.Bind<bool>("HoneyPot", "(DEBUG) PBRsp_alpha_blend - opt1", false, "[Relevant items reloading required] Testing if Shader Forge/PBRsp_texture_alpha (PH) is good enough subsitution for PBRsp_alpha_blend (HS1). Some hairs might need it.");
            pbrspAlphaBlend_2 = Config.Bind<bool>("HoneyPot", "(DEBUG) PBRsp_alpha_blend - opt2", false, "(Not yet implemented) Testing if changing RQ value for originally PBRsp_alpha_blend or lowest rq value renderers can work...");

            forceColor.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.force_color_everything_that_doesnt_have_materialcustoms = forceColor.Value;
            };

            doTransport.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.do_transport = doTransport.Value;
            };

            fixBadTransform.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.fix_bad_transform = fixBadTransform.Value;
            };

            pbrspAlphaBlend_1.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.PBRsp_alpha_blend_mapping_toggle = pbrspAlphaBlend_1.Value;
            };

            pbrspAlphaBlend_2.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.reassign_rq_to_alpha_blend_or_lowest_rq_item = pbrspAlphaBlend_2.Value;
            };

            HoneyPot.force_color_everything_that_doesnt_have_materialcustoms = forceColor.Value;
            HoneyPot.do_transport                                            = doTransport.Value;
            HoneyPot.fix_bad_transform                                       = fixBadTransform.Value;
            HoneyPot.PBRsp_alpha_blend_mapping_toggle                        = pbrspAlphaBlend_1.Value;
            HoneyPot.reassign_rq_to_alpha_blend_or_lowest_rq_item            = pbrspAlphaBlend_2.Value;
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
        private static ConfigEntry<bool> doTransport;
        private static ConfigEntry<bool> fixBadTransform;
        private static ConfigEntry<bool> pbrspAlphaBlend_1;
        private static ConfigEntry<bool> pbrspAlphaBlend_2;

        private Harmony harmony;
        private HoneyPot hp;
    }
}
