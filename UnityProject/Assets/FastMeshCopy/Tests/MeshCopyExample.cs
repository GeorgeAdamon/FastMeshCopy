using UnityEngine;
using UnchartedLimbo.Tools.FastMeshCopy.Runtime;

namespace UnchartedLimbo.Tools.FastMeshCopy.Tests
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshCopyExample : MonoBehaviour
    {
        public Mesh inMesh;
        public Mesh outMesh;
        private MeshFilter mf;

        private void Start()
        {
            mf = GetComponent<MeshFilter>();
            outMesh = new Mesh {name = "Empty Mesh"};
            mf.sharedMesh = outMesh;
        }
        
        private void Update()
         {
             if (!Input.GetKeyDown(KeyCode.Space)) 
                 return;
             
             inMesh.CopyTo(ref outMesh);
             mf.sharedMesh.name = $"Copy of [{inMesh.name}]";
         }
    }
}

