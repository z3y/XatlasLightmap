# XatlasLightmap

A tool for the most efficient lightmap packing in Unity.

## How it works
It generates unique uv2 per mesh, instead of using lightmap UV offsets, which when combined with static batching doesn't create any extra cost. Additional vertex streams are used to set uv2 in editor, which requires modifications to bakery scripts. Lightmap Scale is also calculated differently which allows it to have perfectly uniform texel density when lightmap scale is set to 1.

## How to use
- Add package with git `https://github.com/z3y/XatlasLightmap.git`
- Patch Bakery `Tools > XatlasLightmap > PatchBakery` (only once)
- Add a `XatlasLightmapPacker` script on a GameObject
- Select a BakeryLightmapGroup asset with Packing Mode set to Original UV
- Generate Lightmap UVs on the model importer or create custom UV2
- Enable mesh Read/Write on the model importer
- Pack

[Share your lightmaps](https://github.com/z3y/XatlasLightmap/issues/6)

## Limitations

- Nested lightmap groups arent supported yet
- Doesn't work with GPU instancing, since every mesh ends up being unique. Only use with static batching.
- Model reimports will break the mesh until packed again.
- Since the packing is so efficient and the padding gets applied correctly most shaders will have slight bleeding at 2px, it is recommended to use a centroid interpolator for the lightmap UV.
- Using bicubic sampling requires more padding
- Lightmaps might fail to bake properly first time
- Only works with Bakery
- Sometimes on build it can fail to get UV2 because Read/Write is disabled, even tho its not needed for an editor script. This also doesn't affect the build size. The original mesh is never referenced, only the combined static mesh gets included.

## Examples

Sponza:

![Screenshot 2023-03-18 164316](https://user-images.githubusercontent.com/33181641/227739457-d5bd302d-ba14-4e1f-a745-da5942e1215b.png)

Checker preview:

![image](https://github.com/z3y/XatlasLightmap/assets/33181641/ab5af17a-ef49-442e-96cc-d0cd0295acdd)


Sponza 2:

![image](https://github.com/z3y/XatlasLightmap/assets/33181641/6b791015-51c1-4d12-b0bd-16452dc802bf)
