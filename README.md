# HoneyPot

### Original Author: Yashiro Amamiya

Some notes about this repository: 

A lot of my early monkey patches have been reorganized, but during the development from 1.4.8 to 1.5.0, unforunately the untidiness has gone up significantly again. Partly it's because of my sloppy programming habits, but partly it's because HoneyPot was designed to handle very messy situations: Not really 1-to-1 shader remapping, HS1 mods not exactly conforming to vanilla abdata structures, the differences between HS1 and PH's seemingly identical data format, and inconsistencies in PH's own logic between wear / acce / hair and item handling. Most of these parts are annotated by comments now.

This can be built with .Net Framework 3.5 and the dll references that's currently in the STN 3.1 pack. The current code requires **a publicized version of Assembly-CSharp.dll**; **BepInEx 5.2** and **Harmony 2.** The .csproj file in the repository should be a good indicator of what assemblies/dlls to get. 

Check [here](#1411) for the differences between this and the original (1.4.1 non-opensourced) version. 

## Current Status

- Package releases here: [download page](https://github.com/nx98304/HoneyPot/releases).
- The update log is long, I will not bother anyone here with a huge wall of text. Please just follow the **Requirements & Installations.**
- If you are ＲＥＡＬＬＹ interested, check the update details [here](#150).

### Requirements & Installation
- HoneyPot requires **BepInEx 5.2 or above**. If you somehow are using a PH installation without BepInEx and is trying to convert, please checkout BepInEx releases: https://github.com/BepInEx/BepInEx/releases 
  - **NOTE**: There are other procedures that you must do to convert a fully IPA build to BepInEx + IPA build, which is unrelated to HoneyPot.
- Download from the releases page and extract the zip into PH root folder. Download the **_all** package and just overwrite all files. If you are **upgrading to 1.5.0+ from any versions before (and including) 1.4.7**, please do these in order: 
  - **Remove** `Plugins/PH_Fixes/PH_Fixes_HoneyPot.dll` if it is present. 
  - **Remove or rename** `Plugins/HoneyPot.dll` to something not ending with `.dll` file extension. (And **DO NOT** use the HoneyPot option from the launcher up until (and including) **BR 3.2 Hotfix 1**)
  - **Extract** the HoneyPot package into the PH root folder, **overwrite** everything. 
  - **Delete** `HoneyPot/FileDateTime.txt`, and **run** `HoneyPotInspector.exe`, so that it would reprocess the whole HS1 abdatas.
  - If you have a big file at `abdata/MaterialEditor/HSStandard_PH/hsstandardshaders_for_ph`, feel free to remove it (**Keep the XML!**). The shader abdata has relocated. 

### How to Use
- `HoneyPot/HoneyPotInspector.exe` to generate `HoneyPot/HoneyPotInspector.txt`. **Without this pre-processed txt file, HoneyPot won't work properly.** 
- There are many **options in F1's plugin config menu** for HoneyPot 1.5.0+. Most of them are well-described, please read the notes when toggling them.
- **If you want to make the inspector exe do a complete clean run** (maybe your txt was lost or corrupted, or you are updating across many versions), delete both `HoneyPot/HoneyPotInspector.txt` and `HoneyPot/FileDateMap.txt` first before running inspector. 
- If you see any **Purple Color of Error**, the first thing to do is to run the `HoneyPotInspector.exe` and try again, because you probably forget to do it.
- If you see issues that are not Purple Color of Error, or *still* seeing Purple Color of Error despite having run HoneyPotInspector, please consult the [Known Issues](#known-issues) section.

## Known Issues

### ...that you need some (easy) manual procedures to fix by yourself
- Look into **howto_import_type_definition.zip** in the releases page, if you see any of the following: 
  - Hairs, Clothings, Accessories, Studio items and any other item mods that seem to show up in the list menu in-game, and also show up in the Studio Workspace, but just **don't actually show up in the 3D space**. (Usually means their assetbundle lacks type definitions of `SkinnedMeshRenderer`, `MeshRenderer` or other Renderers from Unity 5.3.x.)
  - Aforementioned items show up in 3D space, but somehow **textures failed to load**, becoming pure/single color only. (Usually means their assetbundle lacks type definitions of `Texture2D` or `Material` from Unity 5.3.x.) 
  - Aforementioned items (especially Hairs) seem to show up just fine, but the **transparency and render queue seem wrong**. (Could be the `SetRenderQueue` Monobehavior's type definition from Unity 5.3.x is lacking.)
  - All above situations will cause error messages like: `The file 'archive:/CAB-...' is corrupted! Remove it and launch unity again!` to be seen in the game's log. Remember checking them. **After investigation, there are no reasonable way to automatically add type definitions to assetbundles.**   
- Water surfaces in scene/map-like studio items looks solid: remove it using SB3U, and put in standalone water items in studio.
- Some clothing has wrong Render Queue settings. If a clothing doesn't actually feature half-transparency (not just net-like or with holes that can see through), you would never want it to have RQ values higher than 2500 (meaning transparent to Unity). It will affect the clothing's ability to receive shadows from other objects. Use SB3U to fix the asset permanently or use Material Editor to save the temporary changes to your character/clothing cards. 
- Most of the **crazy bloom issues** caused by HS1 mods can be fixed by **recalculating tangent** information with Mikkelsen method via SB3U. See: https://discord.com/channels/446784086539763712/446787319228268554/865812589652475915

### ...that are related to Xyth24 "PH mods"
- **Xyth24's PH mods aren't really PH mods**; they are HS1 mods with `.unity3d` file extension removed. Which means `HoneyPotInspector.exe` will not process them at all. 
- The hair part of the Xyth24's head mods don't have issues, because all hairs are forced to use PH hair shaders by default. But accessories and other Xyth24 mods will fail to work if they *pretend* to be PH mods. 
- **Fix step1:** Add `.unity3d` file extension back to Xyth24 abdatas, and run HoneyPotInspector.exe, so it picks up the mods now. 
- **Fix step2:** Add `.unity3d` file extension to the first line of Xyth24 PH lists accordingly. 

### ...in the current functionalities that might be fixed down the line
- Is fur shader remapping possible? 
- There are situations where "1 MaterialCustom needs to correspond to 2 different material/renderer (main/sub)", so sub-color shaders are needed (e.g. using `_Color_3` as the main color) Mostly seen in Roy12 mods, Hard to notice but it's there. Need to port Roy12's HS1 shaders to PH natively (this way a lot of his mods will have significant improvements.)
  - See: https://discord.com/channels/446784086539763712/446787319228268554/871384502595903489
- Importing Alloy/Core series shaders into PH. (I think we already have a couple, but modders like KKUL uses more of them.) 
- It *may* be possible for `HoneyPotInspector.exe` to detect and automate the fixes of crazy bloom issues. Preliminary investigation has begun. See: https://discord.com/channels/446784086539763712/446787088315318303/866319782369296414 

### ...that this mod probably will NEVER fix
- Custom shader-heavy stuff. Like fancy particle effects, complex water effects, etc. 
- Automatically importing type definitions into HS1 abdatas that don't have them. 

## Possible Roadmap

- Find a way to just disable every surface water / ground water HoneyPot detects. There cannot be any sensible way to remap water shaders.
- Further code clean up to make variables in the code humanly readable, and start to annotate the code with comments
- What's up with the Studio Item category changes? I cannot determine the original reason of doing it. 
- Importing Tattoos, Tanlines, Facial makeups, Male brow and other material or texture-based mods. (Currently only Female brows and eyelashes are experimentally supported.)
- It seems to be possible to add Studio item shader remapping to Importer.dll. 

## Update Logs

### 1.5.0

- **BREAKING CHANGE** : 
  - **HoneyPot is now a BepInEx plugin**, requiring at least **BepInEx 5.2** to work. `Plugins/HoneyPot.dll` needs to be removed. 
  - As of **BR 3.2 Hotfix 1**, HoneyPot can no longer be activated or deactivated with the launcher (because it still points to IPA location.)
  - `HoneyPot/HoneyPotInspector.txt` **requires a 100% regenration.** (The format didn't change, however there are so many new shader information added with the new HoneyPotInspector, and HoneyPot really relies on them that you just have to do this.)
- The functionality to duplicate swimsuit into normal clothing categories **now has functioning half-states.** Users also have more control over what to duplicate or not. 
- Chara Maker can now have **an option to use alpha-enabled color picker** for **clothings** and **accessories**. 
- An addition of **Reset Color Key** so that you can reset the color on a colorable (or force-colorable) clothing or accessory, to its modder's original setting (**NOT THE COLORS SAVED IN THE CARD**, if you want to restore colors from a card, simply reload that card would be much easier.) This function is included because PH *remembers* the colors you last picked with color UI. Sometimes this makes it very hard to reset to exactly the clothings' or the accessories' original color. Or sometimes you want to load an old card and restore some of its colors. 
- **ForceColor option** now **no longer cause old cards to have pure white clothings or accessories**. Furthermore, after you activate ForceColor option, you need to use **Force Color Key** to actually activate a clothing or accessory that was non-colorable to be colorable. So there is very low chance to break your old cards in any way.  
- **Fixed** HS1 mods that lack the `(cf|cm)_N_O_root` (or have MULTIPLE of them) causing the clothing fail to be attached correctly.
- **Fixed** certain skinned accessories (such as nails) that were acting weirdly due to BonesFramework code. 
- The `lambert` `clipping` default materials from some HS1 mods **no longer causes the body to become Purple Color of Error**.
- **Fixed** issues where some HS1 hairs would be off-positioned or rotated. Also **hair shaders are now mapped better** because previously I didn't know about the differences between `ObjHair` and `ObjHairAcs`. 
- **Fixed** certain HS1 hairs that need `PBRsp_alpha_blend` equivalent to avoid render queue issues. Now good enough substitute has been found (HSStandard + ALPHABLEND_ON keyword -- **thanks to AgiShark!**).
- Slightly reduce the amount of mods that would cause the crazy bloom or pure black issues by stopping using `PBRsp_3mask` as the default hair / clothing / acce shaders. Some HS1 mods lost their bump map, but they are mostly Roy12's as far as I know, and those mods require porting Roy12's HS1 shaders to PH natively -- which will be worked on next. 
- **AgiShark's continuous support on making HSStandard shaders for PH** better. Also now it's possible to import shader packages to `HoneyPot/shaders/` directly without any code changes (though you would still have to add the mapping manually in `HoneyPot/shader.txt`). 
- Accessories materials and shaders are now better handled to reduce the occurances that colors are applied to materials that should not change colors. Note that some items are set up in a way that needs sub-color shaders or custom shaders that we are still unable to map to.
- Slightly **faster HoneyPot loading time** when game startup. Now there will be a lot of CAB-String error messages during startup, but they are of no consequences, because when that happens, CAB-Strings will be regenerated for those files and successfully loaded just a few seconds later. 
- HoneyPotInspector.exe **no longer leaves a trail of `*.swap` files** after processing HS1 abdatas. 
- **Fixed** a performance bug in Chara Maker that your FPS will be about halved before you open the Clothing menu for the first time. 
- Better mod ID conflict reporting in `UserData/conflict.txt`.
- If a HS1 list provides wrong path to abdata, now HoneyPot will check if the file actually exists before adding them to the runtime database.

### 1.4.8
- **BREAKING CHANGE** : 
  - If you have been using **MoreAccessoriesPH**, you need to **replace** `BepInEx/plugins/MoreAccessories.dll` with the one bundled within this version. Special thanks to Joan6694 for accepting my change back to his codebase and helping prepare the new version!
  - **Remove** `Plugins/PH_Fixes/PH_Fixes_HoneyPot.dll`. 
  - (If you are really worried, be sure to make backups before removal/replacement!)
- The **card face of coordinate cards** now displays all HS mods correctly, no longer just a bunch of purple shapes.
  - **This only affects newly saved coordinate cards.** Load up your old cards, save them again, the card face should display correctly. 
- **F12 key** for _applying_ HoneyPot shaders is **no longer needed** (and no longer overlaps functionality with the in-game console/Unity Runtime Editor). Both in the main game and the studio. 
- Fixed issue where _skinned_ accessories for head/face will conflict with **BonesFramework** code. 
- Improved color customization for both HS and PH clothings/accessories:
  - All accessories are forced-colorable as they have always been, but some color properties were not controlled correctly in the past.
    - Especially some 2-color setup accessories and clothings. 
  - HoneyPot used to force-color all bras and shorts (including PH ones), but that function was lost for some time. Now it's back. 
  - Can further force all non-colorable clothings (including PH ones) by setting `ForceColor=TRUE` in `UserData/modprefs.ini`.
    - **This will make some of your old cards' and scenes' clothings to change color.** You can just change the color back in chara maker because they are now adjustable. If you find this troublesome, just don't save anything and remove this modpref option.
  - Non-colorable clothings & accessories usually imply their materials have only 1 main color to begin with. So in the color UI, the sub color options will show up, but are mostly useless. 
- A note: the **Standard_555** shader that was added back in 1.4.6 was not transplanted from Unity 5.3.x. Thanks AgiShark for pointing this out. 

### 1.4.7

- **Both PH clothings and HS clothing mods with the concept of additional bones** is now supported by HoneyPot. This means HS mods based on **BonesFramework** will work as intended, at the same time, PH clothings that have similar function, while don't have `additional_bones` data in their assetbundles, will also work. Which means the `Activate Bonemod` option in the Better Repack **is superceded by this, and the substitution of** `resources.assets` **is no longer needed.**
- SetRenderQueue behaviour fix. There was a situation where a mismatch of SRQ MBs and their corresponding renderers might happen. 
- Further fixed situations where `null` **Root Bone** is present in clothings. The initial fix was introduced in v1.4.5, but was incorrectly implemented and will impact *male* clothings that are missing the root bone setting. 

### 1.4.6
- **HoneyPot/HoneyPotInspector.exe updated!** When run, it will generate another file called `HoneyPot/FileDateTime.txt` to keep track of all your `.unity3d` files in `abdata/` folder. If the file is found and the creation time of the file is the same, it will skip processing. This vastly speed up the subsequent runs of the inspector exe. Now, please **DO NOT DELETE** `HoneyPot/HoneyPotInspector.txt` from the previous run, before executing the inspector again.   
- **HSStandard** shader ported to Unity **5.5.5**, and making HoneyPot utilize them. (Thanks to AgiShark!) The `HoneyPot/shader.txt` file updated to reflect the addition.
- ~~The **Standard** shader from Unity **5.3.x** also ported to Unity **5.5.5**~~ A modified version of Unity 5.5.5 Standard shader is included, as the vanilla 5.5 version is not entirely compatible with HS items using the 5.3 Standard shader somehow. (Also Thanks to AgiShark!) The `HoneyPot/shader.txt` file updated to reflect the addition (Standard_555).
- Patched `Wears.UpdateShow(WEAR_SHOW_TYPE)` to reflect the fact that HS **tops can show bra when in full-state**. Now both HS top mods and PH top mods, with their correct list settings respectively, can utilize this feature in PH.   
- Fixed HS Top and Bottom clothings interaction. Some of the tops that were meant to disable bottoms, but it disabled shorts instead in 1.4.5.

### 1.4.5
- Basic nip and liquid state handling for HS clothings. HS tops still can't show bras at the same time (still have to go into half-state to see), but this change opens up further efforts that can be done. 
- Fixed issues caused by inspector.txt and the game getting differing upper/lower case filenames. This used to lead to user already generated inspector.txt, but still seeing the Purple Color of Error. (There are still **very** rare cases where mods trying to change the body to use unexpected materials, and it would still show purple error. I currently only know ASH's AF Blade Runner does this.)
- Fixed issues caused by some clothings and accessories have `null` **Root Bone** setting, making clothing not being worn correctly. HoneyPot patched main game through Harmony that it knows to search for "cf_J_Root" and "cm_J_Root" if needed.
- Fixed an issue where some female accessories were not loaded because HoneyPot forgot to look at the correct list data cell.
- shader.txt updated to deal with some new shader mappings.  
- Try to have better fallback shaders and guesses when dealing with studio items, clothing and accessories. 
- Fixed an issue from 1.4.4 caused by changing material properties before assigning material.shader, nullifying the property changes.

### 1.4.4
- **BREAKING CHANGE** : You cannot use shader.txt from 1.4.3 or earlier. It is now much easier to expand shader.txt records, but 1.4.4+ and 1.4.3- are incompatible. 
- A ph_shaders.unity3d is now embedded in the HoneyPot.dll. This file is based on the one from Material Editor's repository: https://github.com/DeathWeasel1337/KK_Plugins/blob/master/src/MaterialEditor.PH/Resources/ph_shaders.unity3d ; many thanks to the help I received from DeathWeasel1337!
- Improved HoneyPot loading speed when game start up, scene change in studio and level change in the main game. And possibly lowered the memory footprint (before this HoneyPot was loaded multiple times.)
- Fixed an issue in 1.4.3 where some of the glass materials are broken again. Some might still be broken -- but it's possible to easily add records to shader.txt now. See [How to use shader.txt effectively](#how-to-use-shadertxt-effectively).

### 1.4.3
- Inspector.exe can now identify shaders at the same capacity as SB3U. If it cannot identify the name, it will record PathID instead. This makes it possible to further expand shader.txt for arbitrary remapping targets. 
- Making sure hair, wear, acc and studio item all go through the same RQ assignment process. Not only "hair-like" things such as horse mane, thatched roof etc, HoneyPot now respects the CRQ value and SRQ MB value (higher priority) of all imported items. So if there are still render queue issues, we can be almost certain that it's the mod itself has the wrong settings, and should be easily adjustable by editing the HS mod with SB3U directly. 
- HS Male hair support. 
- HS Male top cloth which subsitiute the whole body no longer stays purple. 

### 1.4.2
- Fixed an issue where when in chara maker, you won't see the color options right after choosing a piece of HS clothing. 
- Added basic support for HS particle effects for Studio. If the particle effects are simple, the current shader remapping *may* work. If the effects are very complicated, comprising of custom Monobehaviors, distortion effects, material projectors and more, this will 99.9% *NOT* work. 

### 1.4.1.3
- Making most trees/leaves and glasses-like materials work. The materials has to be basic -- any custom materials that are not in vanilla HS build will probably find no substitution in PH. 
- Making the SetItemShader's guessing process somewhat more robust, and make the warning logs much more readable. It will use the inspector-to-preset mapping first, and if that fails, try to guess a few important/often-used alpha materials based on Shader Keywords. 
- Importing HS particle effects mods would be the next thing to try, but it will be in a very limited fashion. 

### 1.4.1.2
- HoneyPot no longer adds FK control points to any items that is loaded with an active animator, preventing breaking the built-in animation when forceably adding the FK controls. 

### 1.4.1.1
- Female hair SetRenderQueue / CustomRenderQueue issue fixes
- Basic HS male clothes support
- Color discrepancy between what's loaded on the character and what's in the color picker UI fix. 
- Experimental HS female eyebrow and eyelash support. 
- No NullReferenceException I know so far
- A couple of small code cleanups.
