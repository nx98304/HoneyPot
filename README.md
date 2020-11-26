# HoneyPot

### Original Author: Yashiro Amamiya

Some notes about this repository: 

A lot of my fixes are ugly and need further adjustments, and there are also a lot of empty functions presumably left by Amamiya-san when he was still working on the project. 

This can be built with .Net Framework 3.5 and the dll references that's currently in the STN 3.1 pack. The .csproj file in the repository should be a good indicator of what assemblies/dlls to get. Note that I had to change all Harmony 1.0.9 usages to version 2 usages, because it was mixing here and there and you won't be able to build it when it was decompiled by dnSpy. 

I have also changed the version number starting from 1.4.1.1. Check [here](#1411) for the differences between this and the original version. 

## Current Status

### 1.4.2
- Package releases here: [download page](https://github.com/nx98304/HoneyPot/releases).
- Fixed an issue where when in chara maker, you won't see the color options right after choosing a piece of HS clothing. 
- Added basic support for HS particle effects for Studio. If the particle effects are simple, the current shader remapping *may* work. If the effects are very complicated, comprising of custom Monobehaviors, distortion effects, material projectors and more, this will 99.9% *NOT* work. 

### Important Accompanying Files
- If you downloaded the latest package from the release page, all the following files are already there.
- For HS hair that has CustomRenderQueue (meaning: When inspecting the hair prefab with SB3Utility, the hair doesn't use SetRenderQueue Monobehavior, but it has seemingly correct CustomRenderQueue values set to each hair mesh's materials, then it will need an specialized version of HoneyPotInspector.exe to help HoneyPot prepare that info in-game. Specialized version here: https://mega.nz/file/oqZgjBaA#5KileCYsM06witMoHlIVZavz63uFH_GC2EJ1EJ3jylk , and regenerate the HoneyPotInspector.txt file )
- If you want to use the experimental particle effect support, also download the HoneyPotInspector.exe above.
- shader.txt was updated for experimental particle effect shader remapping: https://mega.nz/file/NiYwBT7A#L3JaVGA59F00o8kssdyxWrxQtx_yuPsobYADiK5ol1s

## Possible Roadmap

Now that we have made the decompiled source compile again and maintains all the previous fixes without creating new issues, we can gradually aim for future updates: 

- Determine what is the limitation of guessing and remapping Studio Item's shaders. 
- Try to figure out a better way to anticipating HS clothing's custom color material properties. Right now it will yell at you for mismatching the material properties, and some colors will act weirdly if you try to change them, and a lot of items & clothing sets will have custom color UI, but nothing changes when you try to change the color. (usually mean it wasn't supposed to be changed to begin with, even though HoneyPot forces color-changability to all HS clothing.) 
- Further code clean up to make variables in the code humanly readable, and start to annotate the code with comments
- What's up with the Studio Item category changes? I cannot determine the original reason of doing it. 
- Moving the plugin from IPA to fully BepinEx. 
- Performance optimizations? Not sure what exactly can be done about it, since most time it spends on is reading the lists and loading the assets, which would be expected to be slow. 
- Welcome further comments about the project. 

## Past updates

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
