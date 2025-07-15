## xatlas

[![Actions Status](https://github.com/jpcy/xatlas/workflows/build/badge.svg)](https://github.com/jpcy/xatlas/actions) [![Appveyor CI Build Status](https://ci.appveyor.com/api/projects/status/github/jpcy/xatlas?branch=master&svg=true)](https://ci.appveyor.com/project/jpcy/xatlas) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

xatlas is a small C++11 library with no external dependencies that generates unique texture coordinates suitable for baking lightmaps or texture painting.

It is an independent fork of [thekla_atlas](https://github.com/Thekla/thekla_atlas), used by [The Witness](https://en.wikipedia.org/wiki/The_Witness_(2016_video_game)).

## Screenshots

#### Example - [Cesium Milk Truck](https://github.com/KhronosGroup/glTF-Sample-Models)
| Viewer | Random packing | Brute force packing |
|---|---|---|
| [![Viewer](https://user-images.githubusercontent.com/3744372/69908461-48cace80-143e-11ea-8b73-efea5a9f036e.png)](https://user-images.githubusercontent.com/3744372/69908460-48323800-143e-11ea-8b18-58087493c8e9.png) | ![Random packing](https://user-images.githubusercontent.com/3744372/68638607-d4db8b80-054d-11ea-8238-845d94789a2d.gif) | ![Brute force packing](https://user-images.githubusercontent.com/3744372/68638614-da38d600-054d-11ea-82d9-43e558c46d50.gif) |

#### Example - [Godot Third Person Shooter demo](https://github.com/godotengine/tps-demo)
[![Godot TPS](https://user-images.githubusercontent.com/3744372/69908463-48cace80-143e-11ea-8035-b669d1a455f6.png)](https://user-images.githubusercontent.com/3744372/69908462-48cace80-143e-11ea-8946-a2c596ec8028.png)

#### [Graphite/Geogram](http://alice.loria.fr/index.php?option=com_content&view=article&id=22)
![Graphite/Geogram](https://user-images.githubusercontent.com/19478253/69903392-c0deb900-1398-11ea-8a52-c211bc7803a9.gif)

## How to use

### Building

Premake is used. For CMake support, see [here](https://github.com/cpp-pm/xatlas).

Integration into an existing build is simple, only `xatlas.cpp` and `xatlas.h` are required. They can be found in [source/xatlas](https://github.com/jpcy/xatlas/blob/master/source/xatlas)

#### Windows

Run `bin\premake.bat`. Open `build\vs2019\xatlas.sln`.

Note: change the build configuration to "Release". The default - "Debug" - severely degrades performance.

#### Linux

Required packages: `libgl1-mesa-dev libgtk-3-dev xorg-dev`.

Install Premake version 5. Run `premake5 gmake`, `cd build/gmake`, `make`.

### Bindings

[Python](https://github.com/mworchel/xatlas-python)

### Generate an atlas (simple API)

1. Create an empty atlas with `xatlas::Create`.
2. Add one or more meshes with `xatlas::AddMesh`.
3. Call `xatlas::Generate`. Meshes are segmented into charts, which are parameterized and packed into an atlas.

The `xatlas::Atlas` instance created in the first step now contains the result: each input mesh added by `xatlas::AddMesh` has a corresponding new mesh with a UV channel. New meshes have more vertices (the UV channel adds seams), but the same number of indices.

Cleanup with `xatlas::Destroy`.

[Example code here.](https://github.com/jpcy/xatlas/blob/master/source/examples/example.cpp)

### Generate an atlas (tools/editor integration API)

Instead of calling `xatlas::Generate`, the following functions can be called in sequence:

1. `xatlas::ComputeCharts`: meshes are segmented into charts and parameterized.
2. `xatlas::PackCharts`: charts are packed into one or more atlases.

All of these functions take a progress callback. Return false to cancel.

You can call any of these functions multiple times, followed by the proceeding functions, to re-generate the atlas. E.g. calling `xatlas::PackCharts` multiple times to tweak options like unit to texel scale and resolution.

See the [viewer](https://github.com/jpcy/xatlas/tree/master/source/examples/viewer) for example code.

### Pack multiple atlases into a single atlas

1. Create an empty atlas with `xatlas::Create`.
2. Add one or more meshes with `xatlas::AddUvMesh`.
3. Call `xatlas::PackCharts`.

[Example code here.](https://github.com/jpcy/xatlas/blob/master/source/examples/example_uvmesh.cpp)

## Technical information / related publications

[Ignacio Castaño's blog post on thekla_atlas](http://www.ludicon.com/castano/blog/articles/lightmap-parameterization/)

P. Sander, J. Snyder, S. Gortler, and H. Hoppe. [Texture Mapping Progressive Meshes](http://hhoppe.com/proj/tmpm/)

K. Hormann, B. Lévy, and A. Sheffer. [Mesh Parameterization: Theory and Practice](http://alice.loria.fr/publications/papers/2007/SigCourseParam/param-course.pdf)

P. Sander, Z. Wood, S. Gortler, J. Snyder, and H. Hoppe. [Multi-Chart Geometry Images](http://hhoppe.com/proj/mcgim/)

D. Julius, V. Kraevoy, and A. Sheffer. [D-Charts: Quasi-Developable Mesh Segmentation](https://www.cs.ubc.ca/~vlady/dcharts/EG05.pdf)

B. Lévy, S. Petitjean, N. Ray, and J. Maillot. [Least Squares Conformal Maps for Automatic Texture Atlas Generation](https://members.loria.fr/Bruno.Levy/papers/LSCM_SIGGRAPH_2002.pdf)

O. Sorkine, D. Cohen-Or, R. Goldenthal, and D. Lischinski. [Bounded-distortion Piecewise Mesh Parameterization](https://igl.ethz.ch/projects/parameterization/BDPMP/index.php)

Y. O’Donnell. [Precomputed Global Illumination in Frostbite](https://media.contentapi.ea.com/content/dam/eacom/frostbite/files/gdc2018-precomputedgiobalilluminationinfrostbite.pdf)

## Used by

[ArmorPaint](https://armorpaint.org/index.html)

[Bakery - GPU Lightmapper](https://assetstore.unity.com/packages/tools/level-design/bakery-gpu-lightmapper-122218)

[DXR Ambient Occlusion Baking](https://github.com/Twinklebear/dxr-ao-bake) - A demo of ambient occlusion map baking using DXR inline ray tracing.

[Filament](https://google.github.io/filament/)

[Godot Engine](https://github.com/godotengine/godot)

[Graphite/Geogram](http://alice.loria.fr/index.php?option=com_content&view=article&id=22)

[Lightmaps - An OpenGL sample demonstrating path traced lightmap baking on the CPU with Embree](https://github.com/diharaw/Lightmaps)

[redner](https://github.com/BachiLi/redner)

[Skylicht Engine](https://github.com/skylicht-lab/skylicht-engine)

[toy](https://github.com/hugoam/toy) / [two](https://github.com/hugoam/two)

[UNIGINE](https://unigine.com/) - [video](https://www.youtube.com/watch?v=S0gR9T1tWPg)

[Wicked Engine](https://github.com/turanszkij/WickedEngine)

## Related projects

[aobaker](https://github.com/prideout/aobaker) - Ambient occlusion baking. Uses [thekla_atlas](https://github.com/Thekla/thekla_atlas).

[Lightmapper](https://github.com/ands/lightmapper) - Hemicube based lightmap baking. The example model texture coordinates were generated by [thekla_atlas](https://github.com/Thekla/thekla_atlas).

[Microsoft's UVAtlas](https://github.com/Microsoft/UVAtlas) - isochart texture atlasing.

[Ministry of Flat](http://www.quelsolaar.com/ministry_of_flat/) - Commercial automated UV unwrapper.

[seamoptimizer](https://github.com/ands/seamoptimizer) - A C/C++ single-file library that minimizes the hard transition errors of disjoint edges in lightmaps.

[simpleuv](https://github.com/huxingyi/simpleuv/) - Automatic UV Unwrapping Library for Dust3D.

## Models used

[Gazebo model](https://opengameart.org/content/gazebo-0) by Teh_Bucket
