using System;
using System.Reflection;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ClassLibrary4
{
    [BepInPlugin("BepInEx.Honeypot", "HoneyPot", "1.6.0")]
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
            forceColor        = Config.Bind<bool>("HoneyPot", "ForceColor", false, "[Relevant items reloading required] Enable to force-colorable to all clothing/acce (not limited to HS1 ones); BRAs & SHORTs are force-colorable regardless.\n\nDue to how HoneyPot works, it's possible that some leftover color parameters would be in your card data that is not currently used -- the leftover colors will show up when you activate ForceColor when applicable.\n\nIMPORTANCE NOTICE:\nStudioClothesEditor's color-lock function doesn't work well with HoneyPot currently. For Clothings, you DO NOT want to use color-lock. For Accessories, you DO want to color-lock first before using Force Color Key OR Reset Color Key when clicking on a new acce item, and don't forget click on 'Current color base' before unlocking-color. Do not ask me why.");
            entryDuplicate    = Config.Bind("HoneyPot", "Duplicate clothing entries (Restart Required)", HoneyPot.CLOTHING_ENTRY_DUPLICATE.NONE, "[Game restart required] Enable to duplicate clothing entries into other entries for more flexible clothing setup.\n\nPlease understand that the duplicated entries do need to occupy difference ID ranges, and if you have a lot of clothings to duplicate, some might fail due to ID conflicts, and excess entries in a category may slow down opening of such category's sub-menu.");
            hairHSStandard    = Config.Bind<bool>("HoneyPot", "Hair: Keep HSStandard on HS1 hairs", true, "[Relevant items reloading required] PH hair shaders are generally better than HS1 shaders even for HS1 hairs, but some hairs that were using HSStandard will NEVER work with PH hair shaders without render queue issue or weird highlights. Use this option with your own judgement.\n\nDo note that the built in PH color options in the hair category doesn't support HSStandard well, you will need Material Editor. Also if prior to activate this option your card is already saved with Material Editor data, Material Editor probably will take higher priority.");
            colorPickerAlphaWearAcce = Config.Bind<bool>("HoneyPot", "Color Picker Alpha", true, "[Relaunching Chara Maker required] Allow alpha value in Clothing & Accessory menu's color pickers.");
            forceColorKey     = Config.Bind("HoneyPot", "Force Color Key", new KeyboardShortcut(KeyCode.LeftShift), "[ForceColor option required] To enable coloring of non-colorable clothings.\n\n[HOLD DOWN] this key when clicking on the clothing/acce of choice in Chara Maker, or in the StudioClothesEditor.\n\nWhen this key is not held down, color options will not be added to non-colorable clothing/acce.");
            resetColorKey     = Config.Bind("HoneyPot", "Reset Color Key", new KeyboardShortcut(KeyCode.LeftControl), "To load clothings with their original color (modder's original setting, NOT WHAT WAS IN THE CARD).\n\n[HOLD DOWN] this key when clicking on the clothing/acce of choice in Chara Maker, or in the StudioClothesEditor.");

            forceColor.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.force_color = forceColor.Value;
            };

            entryDuplicate.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.entry_duplicate_flags = entryDuplicate.Value;
            };

            hairHSStandard.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.hsstandard_on_hair = hairHSStandard.Value;
            };

            colorPickerAlphaWearAcce.SettingChanged += delegate (object sender, EventArgs args)
            {
                EditMode_CreateColorChangeButton_Patch.allow_alpha = colorPickerAlphaWearAcce.Value;
            };

            forceColorKey.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.force_color_key = forceColorKey.Value;
            };

            resetColorKey.SettingChanged += delegate (object sender, EventArgs args)
            {
                HoneyPot.reset_color_key = resetColorKey.Value;
            };

            HoneyPot.force_color                                = forceColor.Value;
            HoneyPot.entry_duplicate_flags                      = entryDuplicate.Value;
            HoneyPot.hsstandard_on_hair                         = hairHSStandard.Value;
            EditMode_CreateColorChangeButton_Patch.allow_alpha  = colorPickerAlphaWearAcce.Value;
            HoneyPot.force_color_key                            = forceColorKey.Value;
            HoneyPot.reset_color_key                            = resetColorKey.Value;
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
        private static ConfigEntry<bool> hairHSStandard;
        private static ConfigEntry<bool> colorPickerAlphaWearAcce;
        private static ConfigEntry<KeyboardShortcut> forceColorKey;
        private static ConfigEntry<KeyboardShortcut> resetColorKey;

        private Harmony harmony;
        private HoneyPot hp;
    }
}
