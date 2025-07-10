using System;
using UnityEngine;
using UnchartedLimbo.Tools.FastMeshCopy.Runtime;
using Unity.Collections;
using Unity.Mathematics;

namespace UnchartedLimbo.Tools.FastMeshCopy.Tests
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshCopyExample : MonoBehaviour
    {
        public enum CopyType
        {
            Simple,
            Copy_Replicate
        }
        
        public Mesh inMesh;
        public Mesh outMesh;
        public CopyType copyType;
        
        private MeshFilter mf;
        private NativeArray<float4x4> _matrices;
        
        private void Start()
        {
            mf = GetComponent<MeshFilter>();
            
            _matrices = new NativeArray<float4x4>(3, Allocator.Persistent)
            {
                [0] = Matrix4x4.Translate(new Vector3(10, 10, 10)),
                [1] = Matrix4x4.Translate(new Vector3(20, 20, 20)),
                [2] = Matrix4x4.Translate(new Vector3(30, 30, 30)),
            };
        }
        
        private void Update()
         {
             if (!Input.GetKeyDown(KeyCode.Space)) 
                 return;

             switch (copyType)
             {
                 case CopyType.Simple:
                     inMesh.CopyTo(ref outMesh);
                     break;
                 case CopyType.Copy_Replicate:
                     inMesh.CopyReplicate(ref outMesh, _matrices);
                     break;
                 default:
                     throw new ArgumentOutOfRangeException();
             }
             
             mf.sharedMesh = outMesh;
         }

        private void OnDestroy()
        {
            _matrices.Dispose();
        }
    }
}

