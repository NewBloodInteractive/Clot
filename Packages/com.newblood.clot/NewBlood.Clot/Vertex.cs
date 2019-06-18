using UnityEngine;

namespace NewBlood.Clot
{
    public struct Vertex
    {
        public Vector2 TexCoord { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Normal { get; set; }
        public Vector4 Tangent { get; set; }
        public BoneWeight Weight { get; set; }
        public Color32 Color { get; set; }
    }
}
