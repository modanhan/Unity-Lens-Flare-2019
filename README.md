# Unity-Lens-Flare-2019

![Sample Image](https://github.com/modanhan/Unity-Lens-Flare-2019/blob/master/Lens%20Flare%202019.PNG)

## Usage
This effect uses the framework of [Unity Post Processing Stack v2](https://github.com/Unity-Technologies/PostProcessing), so simply add it to your profile. You can find it in Aerobox/Flare.

## Building
If you are building your project with this effect, be sure to add the "Flare" shader to the "Always Included Shaders" list in "Edit/Project Settings/Graphics".

## Caveats
1. This effect is not physically based, so just because you have a physically based project setup doesn't mean this effect will look right in your project. Tweaks are expect to be done on a per project (if not a per scene) basis. Depending on the scope of your project this supporting this effects can spell tech debt.
2. Tweaking this effect can not be done in runtime, edits and only be made in shader. This is mostly because these settings (e.g. intensity, amount of chromatic aberration) don't make sense to be blended, which the Post Processing framework does.
3. I have not benchmarked this since porting it over from previous Unity post processing frameworks.
