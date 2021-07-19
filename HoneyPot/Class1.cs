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

            forceColor  = Config.Bind<bool>("HoneyPot", "ForceColor", false, "[Relevant items reloading required] Enable to force-colorable to all clothings (not limited to HS1 ones); bras, shorts and accessories are force-colorable regardless.");
            doTransport = Config.Bind<bool>("HoneyPot", "DoTransport (Restart Required)", false, "[Game restart required] Enable to duplicate all swimsuits into bras and shorts categories, among other things...");

            forceColor.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.force_color_everything_that_doesnt_have_materialcustoms = forceColor.Value;
            };

            doTransport.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.do_transport = forceColor.Value;
            };

            HoneyPot.force_color_everything_that_doesnt_have_materialcustoms = forceColor.Value;
            HoneyPot.do_transport = doTransport.Value;
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

        private Harmony harmony;
        private HoneyPot hp;
    }
}
