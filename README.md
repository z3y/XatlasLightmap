# XatlasLightmap

- Patch Bakery `Tools/XatlasLightmap/PatchBakery`
- Add a `XatlasLightmapPacker` script on a GameObject
- Add GameObjects to the list and press Pack (all active renderers are taken from child GameObjects)
- Make sure the same GameObjects are on one bakery lightmap group with no UV adjustments (PackingMode: OriginalUV)
- Bake

If the mesh looks wrong after changing it, clear the streams and pack again

![Screenshot 2023-03-18 164316](https://user-images.githubusercontent.com/33181641/227739457-d5bd302d-ba14-4e1f-a745-da5942e1215b.png)
