using System.IO;
using UnityEngine;

namespace NewBlood.Clot
{ 
    static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter writer, Vector2 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
        }

        public static void Write(this BinaryWriter writer, Vector3 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
        }

        public static void Write(this BinaryWriter writer, Vector4 v)
        {
            writer.Write(v.x);
            writer.Write(v.y);
            writer.Write(v.z);
            writer.Write(v.w);
        }

        public static void Write(this BinaryWriter writer, Quaternion q)
        {
            writer.Write(q.x);
            writer.Write(q.y);
            writer.Write(q.z);
            writer.Write(q.w);
        }
    }
}
