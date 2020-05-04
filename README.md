# FastMeshCopy
Zero-allocation copying of Meshes using the new [**MeshData**](https://docs.unity3d.com/2020.1/Documentation/ScriptReference/Mesh.AllocateWritableMeshData.html) functionality in Unity 2020.1+ .

The code comes in the form of a static class with the 
```csharp
public static void CopyTo(this Mesh inMesh, ref Mesh outMesh){...}
``` 
extension method. Simply call this method on a Mesh instance to perform the copy.

## Usage of the example
- Copy both [MeshCopyExample.cs](FastMeshCopy/MeshCopyExample.cs) and [MeshCopyExample.cs](FastMeshCopy/FastMeshCopy.cs) in your Unity Project.
- Attach the [MeshCopyExample.cs](FastMeshCopy/MeshCopyExample.cs) to a GameObject, and reference a Mesh in the inMesh field.
- Run the game, and press the **Spacebar** to perform the mesh copy.


## Limitations
Currently copying BlendShapes and BoneWeights is not supported by Unity's API, so this zero-allocation effort ignores them for the time being.

## Performance Example
Copying of a 98 MB mesh, creating only 80 Bytes of allocations for the Garbage Collector to clean-up.
![](/img/profiler_result.jpg)


