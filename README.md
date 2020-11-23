# HoneyPot

### Original Author: Yashiro Amamiya

Some notes about this repository: 

A lot of my fixes are ugly and need further adjustments, and there are also a lot of empty functions presumably left by Amamiya-san when he was still working on the project. 

This can be built with .Net Framework 3.5 and the dll references that's currently in the STN 3.1 pack. The .csproj file in the repository should be a good indicator of what assemblies/dlls to get. Note that I had to change all Harmony 1.0.9 usages to version 2 usages, because it was mixing here and there and you won't be able to build it when it was decompiled by dnSpy. 

I have also changed the version number starting from 1.4.1.1. Check [here](#1411) for the differences between this and the original version. 

## Current Status

### 1.4.1.2
- HoneyPot no longer adds FK control points to any items that is loaded with an active animator, preventing breaking the built-in animation when forceably adding the FK controls. 

### Note
If your HS hair has CustomRenderQueue (meaning: When inspecting the hair prefab with SB3Utility, the hair doesn't use SetRenderQueue Monobehavior, but it has seemingly correct CustomRenderQueue values set to each hair mesh's materials, then it will need an specialized version of HoneyPotInspector.exe to help HoneyPot prepare that info in-game. Specialized version here: https://mega.nz/file/liASiBKA#OX0FPlv4MhrXTHJzHzWnDEou1Fex4N6RXLDTFxadIGM , and regenerate the HoneyPotInspector.txt file )

## Possible Roadmap
Now that we have made the decompiled source compile again and maintains all the previous fixes without creating new issues, we can gradually aim for future updates: 

- Try to figure out if there's anything to be done with Studio Items' missing shaders. 
- Try to figure out a better way to anticipating HS clothing's custom color material properties. Right now it will yell at you for mismatching the material properties, and some colors will act weirdly if you try to change them, and a lot of items / clothing sets will have custom color UI, but nothing changes when you try to change the color. (usually mean it wasn't supposed to be changed to begin with, even though HoneyPot forces color-changability to all HS clothing.) 
- Further code clean up to make variables in the code humanly readable, and start to annotate the code with comments
- What's up with the Studio Item category changes? I cannot determine the original reason of doing it. 
- Moving the plugin from IPA to fully BepinEx. 
- Performance optimizations? Not sure what exactly can be done about it, since most time it spends on is reading the lists and loading the assets, which would be expected to be slow. 
- Welcome further comments about the project. 

## Past updates

### 1.4.1.1
- Female hair SetRenderQueue / CustomRenderQueue issue fixes (the CustomRenderQueue part require a small update to HoneyPotInspector.exe: https://mega.nz/file/liASiBKA#OX0FPlv4MhrXTHJzHzWnDEou1Fex4N6RXLDTFxadIGM , and regenerate the HoneyPotInspector.txt file)
- Basic HS male clothes support
- Color discrepancy between what's loaded on the character and what's in the color picker UI fix. 
- Experimental HS female eyebrow and eyelash support. 
- No NullReferenceException I know so far
- A couple of small code cleanups.
