# Spine Unity GPU instancing  
## Description
A unity library for spine gpu instancing animation baking  and runtime\
Unity Version: 2022.3.20.f1c1\
Spine Version: 4.1


## Spine Features Supported
✅ Support Bone Animation \
✅ Support Attachment Active\
✅ Support Attachment Sequence \
✅ Support Vertext Color   
✅ Get Bone Position

Anything others not support yet

## How To Use
+ Selectc a sekelton data asset then bake skeleton instancing data
![alt text]([Doc/3b0fda9ba3fdc9f0e3a929740c24f333.png](https://github.com/selecao13/Unity-Spine-Animation-Instancing/blob/master/Doc/3b0fda9ba3fdc9f0e3a929740c24f333.png))

+ Drag baked data to hirachy or scene window\
 ![alt text]([Doc/image.png](https://github.com/selecao13/Unity-Spine-Animation-Instancing/blob/master/Doc/image.png))
 ![alt text]([Doc/image-1.png](https://github.com/selecao13/Unity-Spine-Animation-Instancing/blob/master/Doc/image-1.png))

## Notice
Transparent instancing objects could easily break instancing because other transparent objects cause blocking during rendering.I Suggest to use opaque or alphatest for instancing objects and srp bacher for transparent obejct.Also you can use DrawMeshInstancedor batch render group to impove perfomance.
