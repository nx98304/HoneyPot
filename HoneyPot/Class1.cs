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
            pbrspAlphaBlend_1 = Config.Bind<bool>("HoneyPot", "Hair: PBRsp_alpha_blend to PBRsp_texture_alpha", false, "[Relevant items reloading required] Shader Forge/PBRsp_texture_alpha (PH) can substitute PBRsp_alpha_blend (HS1) to an extent. But it's by no means perfect, so I am making this an option.");
            //Need another option here to allow disabling any _zd like hair meshes.
            //Oh hey, since it's possible to have HSStandard as the true alpha blend shader... use that instead of PBRsp_alpha_blend???
            hairHSStandard    = Config.Bind<bool>("HoneyPot", "Hair: allow using HSStandard shader on hair", true, "[Relevant items reloading required] Some hair that is using HSStandard is just not set up in a way that can use PH hair shaders, but PH hair shaders most of the time are just better than HS shaders, so I am making this an option.");

            forceColor.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.force_color_everything_that_doesnt_have_materialcustoms = forceColor.Value;
            };

            doTransport.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.do_transport = doTransport.Value;
            };

            pbrspAlphaBlend_1.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.PBRsp_alpha_blend_mapping_toggle = pbrspAlphaBlend_1.Value;
            };

            hairHSStandard.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.hsstandard_on_hair = hairHSStandard.Value;
            };

            HoneyPot.force_color_everything_that_doesnt_have_materialcustoms = forceColor.Value;
            HoneyPot.do_transport                                            = doTransport.Value;
            HoneyPot.PBRsp_alpha_blend_mapping_toggle                        = pbrspAlphaBlend_1.Value;
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
        private static ConfigEntry<bool> doTransport;
        private static ConfigEntry<bool> pbrspAlphaBlend_1;
        private static ConfigEntry<bool> hairHSStandard;

        private Harmony harmony;
        private HoneyPot hp;
    }
}
