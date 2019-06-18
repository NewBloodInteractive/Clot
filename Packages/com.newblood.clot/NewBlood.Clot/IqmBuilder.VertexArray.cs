using System.IO;

namespace NewBlood.Clot
{
    public partial class IqmBuilder
    {
        enum VertexArrayFormat
        {
            SByte,
            Byte,
            Int16,
            UInt16,
            Int32,
            UInt32,
            Half,
            Single,
            Double
        }

        enum VertexArrayType
        {
            Position,
            TexCoord,
            Normal,
            Tangent,
            BlendIndexes,
            BlendWeights,
            Color,
            Custom = 0x10
        }

        struct VertexArray
        {
            public VertexArrayType Type;
            public int Flags;
            public VertexArrayFormat Format;
            public int Size;
            public int Offset;

            public static VertexArray FromType(VertexArrayType type)
            {
                var array = new VertexArray { Type = type };

                switch (type)
                {
                case VertexArrayType.TexCoord:
                    array.Size   = 2;
                    array.Format = VertexArrayFormat.Single;
                    break;
                case VertexArrayType.Position:
                case VertexArrayType.Normal:
                    array.Size   = 3;
                    array.Format = VertexArrayFormat.Single;
                    break;
                case VertexArrayType.Tangent:
                    array.Size   = 4;
                    array.Format = VertexArrayFormat.Single;
                    break;
                case VertexArrayType.Color:
                case VertexArrayType.BlendIndexes:
                case VertexArrayType.BlendWeights:
                    array.Size   = 4;
                    array.Format = VertexArrayFormat.Byte;
                    break;
                }

                return array;
            }

            public void Write(BinaryWriter writer)
            {
                writer.Write((int)Type);
                writer.Write(Flags);
                writer.Write((int)Format);
                writer.Write(Size);
                writer.Write(Offset);
            }
        }
    }
}
