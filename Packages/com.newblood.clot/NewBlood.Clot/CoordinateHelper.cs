using UnityEngine;

namespace NewBlood.Clot
{
    public static class CoordinateHelper
    {
        /// <summary>Transforms a <see cref="Vector3"/> from Quake's coordinate system to Unity's.</summary>
        /// <param name="isScale">Specifies whether the vector represents a scale or not.</param>
        public static Vector3 QuakeToUnity(Vector3 v, bool isScale = false) => new Vector3(isScale ? v.y : -v.y, v.z, v.x);

        /// <summary>Transforms a <see cref="Vector4"/> from Quake's coordinate system to Unity's.</summary>
        public static Vector4 QuakeToUnity(Vector4 v) => new Vector4(-v.y, v.z, v.x, v.w);

        /// <summary>Transforms a <see cref="Quaternion"/> from Quake's coordinate system to Unity's.</summary>
        public static Quaternion QuakeToUnity(Quaternion q) => new Quaternion(q.y, -q.z, -q.x, q.w);

        /// <summary>Transforms a <see cref="Vector3"/> from Unity's coordinate system to Quake's.</summary>
        /// <param name="isScale">Specifies whether the vector represents a scale or not.</param>
        public static Vector3 UnityToQuake(Vector3 v, bool isScale = false) => new Vector3(v.z, isScale ? v.x : -v.x, v.y);

        /// <summary>Transforms a <see cref="Vector4"/> from Unity's coordinate system to Quake's.</summary>
        public static Vector4 UnityToQuake(Vector4 v) => new Vector4(v.z, -v.x, v.y, v.w);

        /// <summary>Transforms a <see cref="Quaternion"/> from Unity's coordinate system to Quake's.</summary>
        public static Quaternion UnityToQuake(Quaternion q) => new Quaternion(-q.z, q.x, -q.y, q.w);
    }
}
