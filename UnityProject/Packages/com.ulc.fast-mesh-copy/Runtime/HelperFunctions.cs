using UnityEngine;
using UnityEngine.Rendering;
using static UnchartedLimbo.Tools.FastMeshCopy.Runtime.Constants;


namespace UnchartedLimbo.Tools.FastMeshCopy.Runtime
{
    /// <summary>
    /// Author: George Adamopoulos
    /// Version: 2.0.0
    /// Date: 2021-06-15
    /// License: GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007
    /// </summary>
    public static class HelperFunctions
    {
        /// <summary>
        /// Returns the real size of a <see cref="Mesh"/> vertex in bytes.
        /// This size is determined by the amount, dimensionality and precision of the VertexAttributes describing the
        /// vertices of the mesh.
        /// </summary>
        public static int SizeOfVertex(this Mesh mesh)
        {
            var sourceVertexFormat = mesh.GetVertexAttributes();
            var sourceVertexSize   = 0;

            for (var i = 0; i < sourceVertexFormat.Length; i++)
            {
                // FIGURE OUT THE VERTEX SIZE
                var size = FLOAT_SIZE;

                switch (sourceVertexFormat[i].format)
                {
                    case VertexAttributeFormat.Float16:
                    case VertexAttributeFormat.UNorm16:
                    case VertexAttributeFormat.SNorm16:
                    case VertexAttributeFormat.UInt16:
                    case VertexAttributeFormat.SInt16:
                        size = 2;
                        break;
                    case VertexAttributeFormat.UNorm8:
                    case VertexAttributeFormat.SNorm8:
                    case VertexAttributeFormat.UInt8:
                    case VertexAttributeFormat.SInt8:
                        size = 1;
                        break;
                }

                sourceVertexSize += sourceVertexFormat[i].dimension * size;
            }

            return sourceVertexSize;
        }

        /// <summary>
        /// Checks whether 2 <see cref="Mesh"/> objects have an identical Vertex format.
        /// </summary>
        public static bool EqualsVertexFormat(this Mesh mesh, Mesh other)
        {
            var format  = mesh.GetVertexAttributes();
            var format2 = other.GetVertexAttributes();
            
            if (format.Length != format2.Length)
                return false;

            for (int i = 0; i < format.Length; i++)
            {
                var a = format[i];
                var b = format2[i];

                if (a != b)
                    return false;
            }

            return true;
        }
        
        /// <summary>
        /// Extract the <see cref="VertexAttributeDescriptor"/> of a <see cref="Mesh"/> by optionally storing
        /// every attribute that is NOT position in a separate vertex stream.
        /// </summary>
        public static VertexAttributeDescriptor[] CopyVertexFormat(this Mesh mesh, int positionStream = 0, int otherStream = 1)
        {
            var sourceVertexFormat = mesh.GetVertexAttributes();
            var destVertexFormat   = new VertexAttributeDescriptor[sourceVertexFormat.Length];

            for (var i = 0; i < sourceVertexFormat.Length; i++)
            {

                // CREATE THE NEW VERTEX FORMAT
                // Assign stream 0 only to VertexPosition, and stream 1 to everything else
                var stream           = sourceVertexFormat[i].attribute == VertexAttribute.Position ? 
                        positionStream : otherStream;
                
                destVertexFormat[i] = new VertexAttributeDescriptor(sourceVertexFormat[i].attribute, 
                                                                    sourceVertexFormat[i].format, 
                                                                    sourceVertexFormat[i].dimension, stream);
            }

            return destVertexFormat;
        }

        /// <summary>
        /// Checks whether the vertices of a <see cref="Mesh"/> only store the position attribute.
        /// </summary>
        public static bool IsVertexPositionOnly(this Mesh mesh)
        {
            var sourceVertexFormat = mesh.GetVertexAttributes();
            
            var positionOnly = true;

            for (var i = 0; i < sourceVertexFormat.Length; i++)
            {
                if (sourceVertexFormat[i].attribute != VertexAttribute.Position)
                {
                    positionOnly = false;
                    break;
                }
            }

            return positionOnly;

        }

        /// <summary>
        /// Get the actual index count of a <see cref="Mesh.MeshData"/> object.
        /// </summary>
        public static int GetIndexCount(this Mesh.MeshData md)
        {
            return md.indexFormat == IndexFormat.UInt16
                    ? md.GetIndexData<ushort>().Length
                    : md.GetIndexData<uint>().Length;
        }
    }
}