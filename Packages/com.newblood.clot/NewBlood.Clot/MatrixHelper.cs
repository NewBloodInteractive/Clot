using UnityEngine;

namespace NewBlood.Clot
{
    public class MatrixHelper
    {
        /// <summary>Decomposes a <see cref="Matrix4x4"/> into translate, rotate and scale components.</summary>
        public static void Decompose(in Matrix4x4 matrix, out Vector3 translate, out Quaternion rotate, out Vector3 scale)
        {
            var c0 = matrix.GetColumn(0);
            var c1 = matrix.GetColumn(1);
            var c2 = matrix.GetColumn(2);

            translate = matrix.GetColumn(3);
            rotate    = matrix.rotation;
            scale     = new Vector3(c0.magnitude, c1.magnitude, c2.magnitude);

            if (matrix.determinant < 0) scale.x = -scale.x;
        }
    }
}
