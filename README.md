# XatlasLightmap

This tool sets additional vertex streams on mesh renderers which allows for more efficient packing. It requires a small modification to bakery so that uv2 data from additional vertex streams can also be used when baking. When used with static batching there is no additional cost to the added unique geometry since the meshes get merged anyways.

- Patch Bakery `Tools/XatlasLightmap/PatchBakery`
- Add a `XatlasLightmapPacker` script on a GameObject
- Add GameObjects to the list and press Pack (all active renderers are taken from child GameObjects)
- Make sure the same GameObjects are on one bakery lightmap group with no UV adjustments (PackingMode: OriginalUV)
- Bake

Model reimports are currently not detected, the mesh data will need to be updated each time a mesh changes or it will look completely broken in the editor.

Since the packing is so efficient and the padding gets applied correctly most shaders will have slight bleeding at 2px, it is recommended to use a centroid interpolator for the lightmap uv.

It also handles scaling of uv2 differently which allows it to have perfectly uniform texel density.

Breaks GPU instancing.

Sponza:

![Screenshot 2023-03-18 164316](https://user-images.githubusercontent.com/33181641/227739457-d5bd302d-ba14-4e1f-a745-da5942e1215b.png)

Checker preview:

![image](https://github.com/z3y/XatlasLightmap/assets/33181641/ab5af17a-ef49-442e-96cc-d0cd0295acdd)


Sponza 2:

![image](https://github.com/z3y/XatlasLightmap/assets/33181641/6b791015-51c1-4d12-b0bd-16452dc802bf)
