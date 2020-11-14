# HoneyPot
HoneyPot

Original Author: Yashiro Amamiya

Some notes about this repository: 

A lot of my fixes are ugly and need further adjustments, and there are also a lot of empty functions presumably left by Amamiya-san when he was still working on the project. 

This can be built with .Net Framework 3.5 and the dll references that's currently in the STN 3.1 pack. The .csproj file in the repository should be a good indicator of what assemblies/dlls to get. Note that I had to change all Harmony 1.0.9 usages to version 2 usages, because it was mixing here and there and you won't be able to build it when it was decompiled by dnSpy. 

I have also changed the version number to 1.4.1.1. 

Compared to the original 1.4.0, this version has:
- Female hair SetRenderQueue / CustomRenderQueue issue fixes (the CustomRenderQueue part require a small update to HoneyPotInspector.exe: https://mega.nz/file/liASiBKA#OX0FPlv4MhrXTHJzHzWnDEou1Fex4N6RXLDTFxadIGM , and regenerate the HoneyPotInspector.txt file)
- Basic HS male clothes support
- Color discrepancy between what's loaded on the character and what's in the color picker UI fix. 
- Experimental HS female eyebrow and eyelash support. 
- No NullReferenceException I know so far
- A couple of small code cleanups.
