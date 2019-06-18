using UnityEngine;

namespace NewBlood.Clot
{
    public class Bone
    {
        public Transform Transform { get; set; }

        public string Name { get; set; }

        public Bone Parent { get; set; }

        public Vector3 Translate { get; set; }

        public Quaternion Rotate { get; set; }

        public Vector3 Scale { get; set; }

        public Matrix4x4 Matrix
        {
            get => Matrix4x4.TRS(Translate, Rotate, Scale);

            set
            {
                MatrixHelper.Decompose(value,
                    out var translate,
                    out var rotate,
                    out var scale
                );

                Translate = translate;
                Rotate    = rotate;
                Scale     = scale;
            }
        }
    }
}
