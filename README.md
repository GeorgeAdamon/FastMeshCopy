# FastMeshCopy
Zero-allocation copying of Meshes using the new [**MeshData**](https://docs.unity3d.com/2020.1/Documentation/ScriptReference/Mesh.AllocateWritableMeshData.html) functionality in Unity **2020.1+**.

The code comes in the form of the static class [`FastMeshCopy`](UnityProject/Assets/FastMeshCopy/Runtime/FastMeshCopy.cs) with the 
```csharp
public static void CopyTo(this Mesh inMesh, ref Mesh outMesh){...}
``` 
and
```csharp
public static Mesh CopyReplicate(this Mesh mesh, NativeArray<float4x4> matrices) {...}
```
extension methods.  
Simply call this method on a Mesh instance to perform either a **Single Copy**, or a **Multi-Copy** based on an array of transformations + **Merge**.

## Add as a Unity Package
Add this line to your `Packages/manifest.json` file
```js
"ulc-tools-fastmeshcopy": "https://github.com/GeorgeAdamon/FastMeshCopy.git?path=/UnityProject/Assets/FastMeshCopy#master",
```

## Usage of the Single Copy example
- Attach the [MeshCopyExample.cs](FastMeshCopy/MeshCopyExample.cs) to a GameObject, and reference a Mesh in the inMesh field.
- Run the game, and press the **Spacebar** to perform the mesh copy.


## Limitations
Accessing BlendShapes and BoneWeights without GC allocations is not supported by Unity's API, so this zero-allocation effort **ignores** them for the time being. See the [**discussion**](https://forum.unity.com/threads/feedback-wanted-mesh-scripting-api-improvements.684670/page-3#post-5800297)

## Performance Example
Copying of a 98 MB mesh, creating only 80 Bytes of allocations for the Garbage Collector to clean-up.
![](/img/profiler_result.jpg)


