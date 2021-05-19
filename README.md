# HoneyPot

### Original Author: Yashiro Amamiya

Some notes about this repository: 

A lot of my fixes are ugly and need further adjustments, and there are also a lot of empty functions presumably left by Amamiya-san when he was still working on the project. 

This can be built with .Net Framework 3.5 and the dll references that's currently in the STN 3.1 pack. The .csproj file in the repository should be a good indicator of what assemblies/dlls to get. Note that I had to change all Harmony 1.0.9 usages to version 2 usages, because it was mixing here and there and you won't be able to build it when it was decompiled by dnSpy. 

I have also changed the version number starting from 1.4.1.1. Check [here](#1411) for the differences between this and the original version. 

## Current Status

### 1.4.8
- Package releases here: [download page](https://github.com/nx98304/HoneyPot/releases).
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
- A note: the **Standard_555** shader that was added back in 1.4.6 was not transplanted from Unity 5.3.x. Thanks Doodoo for pointing this out. 

### Requirements & Installation
- HoneyPot is still an IPA mod for now, but make sure your PH installation has **BepInEx**. BepInEx doesn't have to be the latest (up to BepInEx 5.4.5 has been tested. **Newer BepInEx version actually may not work**), as HoneyPot mainly just needs **Harmony v2** in the BepInEx folder.
- Download from the releases page and extract the zip into PH root folder. When in doubt, download the _all package and just overwrite all files. (Backup your `HoneyPot/shader.txt` if needed) ; if you have been keeping updated, download the _trim package that only contain the updated files. 
  - **If you downloaded the _all package, and are upgrading from HoneyPot 1.4.5 or earlier directly**, do a complete clean run by also removing `HoneyPot/HoneyPotInspector.txt`.

### How to Use
- `HoneyPot/HoneyPotInspector.exe` to generate `HoneyPot/HoneyPotInspector.txt`. **Without this pre-processed txt file, HoneyPot won't work properly.** 
- **If you want to make the inspector exe do a complete clean run** (maybe your txt was lost or corrupted), delete both `HoneyPot/HoneyPotInspector.txt` and `HoneyPot/FileDateMap.txt` first before running inspector. 
- If you see any **Purple Color of Error**, the first thing to do is to run the `HoneyPotInspector.exe` and try again, because you probably forget to do it.
- `HoneyPot/shader.txt` to do the shader remapping. You can try to add new shader remapping rules here. See [How to use shader.txt effectively](#how-to-use-shadertxt-effectively). (The Material Editor mod makes it a lot easier to find suitable remap targets). 
- If you see issues that are not Purple Color of Error, or *still* seeing Purple Color of Error despite having run HoneyPotInspector, please consult the [Known Issues](#known-issues) section.
- `UserData/modprefs.ini` options:
  - set `DoTransport=FALSE` to make HoneyPot NOT adding swimsuits to bras and shorts categories in chara maker.
  - set `ForceColor=TRUE` to make HoneyPot make all not-already-colorable clothings colorable. **This will make some of your old cards' and scenes' clothings to change color.** You can just change the color back in chara maker because they are now adjustable. If you find this troublesome, just don't save anything and remove this modpref option.
```
[HoneyPot] 
DoTransport=FALSE
ForceColor=TRUE
```

## Known Issues

### ...that you need some (easy) manual procedures to fix by yourself
- Look into **howto_import_type_definition.zip** in the releases page, if you see any of the following: 
  - Hairs, Clothings, Accessories, Studio items and any other item mods that seem to show up in the list menu in-game, and also show up in the Studio Workspace, but just **don't actually show up in the 3D space**. (Usually means their assetbundle lacks type definitions of `SkinnedMeshRenderer`, `MeshRenderer` or other Renderers from Unity 5.3.x.)
  - Aforementioned items show up in 3D space, but somehow **textures failed to load**, becoming pure/single color only. (Usually means their assetbundle lacks type definitions of `Texture2D` or `Material` from Unity 5.3.x.) 
  - Aforementioned items (especially Hairs) seem to show up just fine, but the **transparency and render queue seem wrong**. (Could be the `SetRenderQueue` Monobehavior's type definition from Unity 5.3.x is lacking.)
  - All above situations will cause error messages like: `The file 'archive:/CAB-...' is corrupted! Remove it and launch unity again!` to be seen in the game's log. Remember checking them. **I don't know how to automatically adding type definitions to assetbundles.**   
- Water surfaces in scene/map-like studio items looks solid: remove it using SB3U, and put in standalone water items in studio.
- Some clothing has wrong Render Queue settings. If a clothing doesn't actually feature half-transparency (not just net-like or with holes that can see through), you would never want it to have RQ values higher than 2500 (meaning transparent to Unity). It will affect the clothing's ability to receive shadows from other objects. Use SB3U to fix the asset permanently or use Material Editor to save the temporary changes to your character/clothing cards. 
- If you encounter HS top clothings that made body skin purple despite having run HoneyPotInspector, currently you will have to use SB3U and remove whatever material that is set on the `o_body_*` meshes manually. The material setting of the body meshes should be `(none)`. 

### ...in the current functionalities that might be fixed down the line
- Detection for some glass-like materials are still wrong. 
- Some HS top clothing has a temporary or almost-empty `Material` assigned to `o_body_*` Which is a big issue for PH's loading process, since those materials are missing a lot of textures and settings from normal, working body materials, and PH's loading process will fail with a lot of NREs. 
- Is fur shader remapping possible? 

### ...that this mod probably will NEVER fix
- Custom shader-heavy stuff. Like fancy particle effects, complex water effects, etc. 

## Possible Roadmap

- Find a way to just disable every surface water / ground water HoneyPot detects. There cannot be any sensible way to remap water shaders.
- Further code clean up to make variables in the code humanly readable, and start to annotate the code with comments
- What's up with the Studio Item category changes? I cannot determine the original reason of doing it. 
- Importing Tattoos, Tanlines, Facial makeups, Male brow and other material or texture-based mods. (Currently only Female brows and eyelashes are experimentally supported.)
- Moving the plugin from IPA to fully BepInEx. 
- Welcome further comments about the project. 

### How to use shader.txt effectively
shader.txt 's remapping records is fairly limited before. But with the now enhanced `HoneyPotInspector.txt` information, a lot of unknown HS shaders now are represented by its **PathID** (for example, `-7902135702503874624`), and you can now remap that PathID to suitable PH shaders. It's fairly easy to find suitable remapping shaders using Material Editor: 
- Check undesirable materials with **Material Editor**, try out different PH shaders (yes, there's still limitation here, you can only choose from that list, unless you know how to add new shaders to Material Editor and HoneyPot). 
- After finding suitable shader, remember the PH shader name and the material name. 
- Go to `HoneyPotInspector.txt`, use search function of any text reader to find the material name, and take note of its HS shader name. 
- Go to `shader.txt`, add a line in the form of `HS_shader_name|PH_shader_name` 
- Multiple HS shaders can map to a single PH shader. Unfortunately if a single HS shader is used on multiple materials, and yet you want to map to different PH shaders, this is currently impossible. 
- After changing `shader.txt` you need to restart the game. 

## Past updates

### 1.4.7

- **Both PH clothings and HS clothing mods with the concept of additional bones** is now supported by HoneyPot. This means HS mods based on **BonesFramework** will work as intended, at the same time, PH clothings that have similar function, while don't have `additional_bones` data in their assetbundles, will also work. Which means the `Activate Bonemod` option in the Better Repack **is superceded by this, and the substitution of** `resources.assets` **is no longer needed.**
- SetRenderQueue behaviour fix. There was a situation where a mismatch of SRQ MBs and their corresponding renderers might happen. 
- Further fixed situations where `null` **Root Bone** is present in clothings. The initial fix was introduced in v1.4.5, but was incorrectly implemented and will impact *male* clothings that are missing the root bone setting. 

### 1.4.6
- **HoneyPot/HoneyPotInspector.exe updated!** When run, it will generate another file called `HoneyPot/FileDateTime.txt` to keep track of all your `.unity3d` files in `abdata/` folder. If the file is found and the creation time of the file is the same, it will skip processing. This vastly speed up the subsequent runs of the inspector exe. Now, please **DO NOT DELETE** `HoneyPot/HoneyPotInspector.txt` from the previous run, before executing the inspector again.   
- **HSStandard** shader ported to Unity **5.5.5**, and making HoneyPot utilize them. (Thanks to Doodoo!) The `HoneyPot/shader.txt` file updated to reflect the addition.
- ~~The **Standard** shader from Unity **5.3.x** also ported to Unity **5.5.5**~~ A modified version of Unity 5.5.5 Standard shader is included, as the vanilla 5.5 version is not entirely compatible with HS items using the 5.3 Standard shader somehow. (Also Thanks to Doodoo!) The `HoneyPot/shader.txt` file updated to reflect the addition (Standard_555).
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
