using UnityEngine;

namespace NewBlood.Clot
{
    static class MatrixExtensions
    {
        /// <summary>Extension method form of <see cref="MatrixHelper.Decompose"/> to allow matrix decomposition via tuple deconstruction.</summary>
        public static void Deconstruct(in this Matrix4x4 matrix, out Vector3 translate, out Quaternion rotate, out Vector3 scale)
            => MatrixHelper.Decompose(in matrix, out translate, out rotate, out scale);

        public static void Decompose(in this Matrix4x4 matrix, out Vector3 translate, out Quaternion rotate, out Vector3 scale)
            => MatrixHelper.Decompose(in matrix, out translate, out rotate, out scale);
    }
}
