using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace NewBlood.Clot
{
    public partial class IqmBuilder
    {
        static readonly byte[] Identifier = Encoding.UTF8.GetBytes("INTERQUAKEMODEL\0");

        readonly Dictionary<int, List<KeyFrame>> frames = new Dictionary<int, List<KeyFrame>>();

        public List<Bone> Bones { get; } = new List<Bone>();

        public List<Vertex> Vertices { get; } = new List<Vertex>();

        public List<Triangle> Triangles { get; } = new List<Triangle>();

        public List<Submesh> Submeshes { get; } = new List<Submesh>();

        public int FrameCount { get; set; }

        public List<AnimationRange> Animations { get; } = new List<AnimationRange>();

        public void ClearAnimations()
        {
            FrameCount = 0;
            frames.Clear();
            Animations.Clear();
        }

        /// <summary>Set an animation key with a supplied transformation matrix.</summary>
        public void SetKey(int frame, Bone bone, Matrix4x4 matrix)
        {
            MatrixHelper.Decompose(matrix, out var translate, out var rotate, out var scale);
            SetKey(frame, bone, translate, rotate, scale);
        }

        /// <summary>Set an animation key with a supplied translation, rotation and scale.</summary>
        public void SetKey(int frame, Bone bone, Vector3 translate, Quaternion rotate, Vector3 scale)
        {
            if (frame >= FrameCount)
                FrameCount = frame + 1;

            if (!frames.TryGetValue(frame, out var keys))
                frames.Add(frame, keys = new List<KeyFrame>());

            var key = new KeyFrame
            {
                Bone      = bone,
                Translate = translate,
                Rotate    = rotate,
                Scale     = scale
            };

            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].Bone == bone)
                {
                    keys[i] = key;
                    return;
                }
            }

            keys.Add(key);
        }

        public void Write(Stream stream)
        {
            using (var ms     = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                var text   = new TextTable();
                var header = new Header { Version = 2 };

                writer.Write(Identifier);
                header.Write(writer);

                WriteBones(writer, text, ref header);
                WriteVertexArrays(writer, ref header);
                WriteTriangles(writer, ref header);
                WriteMeshes(writer, text, ref header);
                WriteAnimations(writer, text, ref header);

                var textArray     = text.ToArray();
                header.TextOffset = (int)ms.Position;
                header.TextCount  = textArray.Length;
                writer.Write(textArray);

                header.FileSize = (int)ms.Length;
                ms.Position     = Identifier.Length;
                header.Write(writer);

                ms.Position = 0;
                ms.CopyTo(stream);
            }
        }

        void WriteAnimations(BinaryWriter writer, TextTable text, ref Header header)
        {
            // todo: make this code nicer

            if (FrameCount <= 0)
                return;

            header.PoseCount  = Bones.Count;
            header.FrameCount = FrameCount;
            var frameData     = new List<ushort>();
            var keyFrames     = new KeyFrame[Bones.Count];
            var channels      = new PoseChannels[FrameCount, Bones.Count];
            var poses         = new Pose[Bones.Count];

            for (int b = 0; b < Bones.Count; b++)
            {
                keyFrames[b] = new KeyFrame
                {
                    Bone      = Bones[b],
                    Translate = Bones[b].Translate,
                    Rotate    = Bones[b].Rotate,
                    Scale     = Bones[b].Scale
                };
            }

            for (int f = 0; f < FrameCount; f++)
            {
                if (frames.TryGetValue(f, out var keys))
                {
                    foreach (var key in keys)
                    {
                        var bone = Bones.IndexOf(key.Bone);
                        if (bone == -1) continue;
                        keyFrames[bone] = key;
                    }
                }

                for (int b = 0; b < Bones.Count; b++)
                {
                    var translate = CoordinateHelper.UnityToQuake(keyFrames[b].Translate);
                    var rotate    = CoordinateHelper.UnityToQuake(keyFrames[b].Rotate);
                    var scale     = CoordinateHelper.UnityToQuake(keyFrames[b].Scale, isScale: true);

                    channels[f, b][0] = translate.x;
                    channels[f, b][1] = translate.y;
                    channels[f, b][2] = translate.z;
                    channels[f, b][3] = rotate.x;
                    channels[f, b][4] = rotate.y;
                    channels[f, b][5] = rotate.z;
                    channels[f, b][6] = rotate.w;
                    channels[f, b][7] = scale.x;
                    channels[f, b][8] = scale.y;
                    channels[f, b][9] = scale.z;

                    for (int c = 0; c < 10; c++)
                    {
                        if (f == 0)
                            poses[b].Min[c] = poses[b].Max[c] = channels[f, b][c];
                        else
                        {
                            poses[b].Min[c] = Mathf.Min(poses[b].Min[c], channels[f, b][c]);
                            poses[b].Max[c] = Mathf.Max(poses[b].Max[c], channels[f, b][c]);
                        }
                    }
                }
            }

            for (int b = 0; b < Bones.Count; b++)
            {
                poses[b].Parent = Bones.IndexOf(Bones[b].Parent);

                for (int c = 0; c < 10; c++)
                {
                    poses[b].ChannelOffset[c] = poses[b].Min[c];
                    poses[b].ChannelScale [c] = 0;

                    if (poses[b].Min[c] == poses[b].Max[c])
                        continue;

                    poses[b].ChannelMask |= 1 << c;
                    poses[b].ChannelScale[c] = (poses[b].Max[c] - poses[b].Min[c]) / 65535f;
                    header.FrameChannelCount++;
                }
            }

            for (int f = 0; f < FrameCount; f++)
            {
                for (int b = 0; b < Bones.Count; b++)
                {
                    for (int c = 0; c < 10; c++)
                    {
                        if ((poses[b].ChannelMask & (1 << c)) == 0)
                            continue;

                        float frame = (channels[f, b][c] - poses[b].Min[c]) / (poses[b].Max[c] - poses[b].Min[c]);
                        frameData.Add((ushort)Mathf.Min(frame * 65535f, 65535f));
                    }
                }
            }

            header.PoseOffset = (int)writer.BaseStream.Position;
            foreach (var pose in poses) pose.Write(writer);

            header.FrameOffset = (int)writer.BaseStream.Position;
            foreach (var frame in frameData) writer.Write(frame);

            header.AnimCount  = Mathf.Max(1, Animations.Count);
            header.AnimOffset = (int)writer.BaseStream.Position;

            if (Animations.Count > 0)
            {
                foreach (var range in Animations)
                    WriteAnimation(range);
            }
            else
            {
                WriteAnimation(new AnimationRange
                {
                    Name       = "Sequence",
                    FrameCount = FrameCount,
                    FrameRate  = 24
                });
            }

            void WriteAnimation(AnimationRange range)
            {
                writer.Write(text.GetOrAddIndex(range.Name));
                writer.Write(range.FrameIndex);
                writer.Write(range.FrameCount);
                writer.Write(range.FrameRate);
                writer.Write((int)range.Flags);
            }
        }

        void WriteMeshes(BinaryWriter writer, TextTable text, ref Header header)
        {
            header.MeshOffset = (int)writer.BaseStream.Position;
            header.MeshCount  = Mathf.Max(1, Submeshes.Count);

            if (Submeshes.Count > 0)
            {
                foreach (var mesh in Submeshes)
                    WriteMesh(mesh);
            }
            else
            {
                WriteMesh(new Submesh
                {
                    Name          = "Model",
                    Material      = "Default",
                    TriangleCount = Triangles.Count
                });
            }

            void WriteMesh(Submesh mesh)
            {
                writer.Write(text.GetOrAddIndex(mesh.Name));
                writer.Write(text.GetOrAddIndex(mesh.Material));
                writer.Write(0);
                writer.Write(Vertices.Count);
                writer.Write(mesh.TriangleIndex);
                writer.Write(mesh.TriangleCount);
            }
        }

        void WriteTriangles(BinaryWriter writer, ref Header header)
        {
            header.TriangleCount  = Triangles.Count;
            header.TriangleOffset = (int)writer.BaseStream.Position;

            foreach (var triangle in Triangles)
            {
                writer.Write(triangle.VertexA);
                writer.Write(triangle.VertexB);
                writer.Write(triangle.VertexC);
            }

            header.AdjacencyOffset = (int)writer.BaseStream.Position;

            foreach (var triangle in Triangles)
            {
                // todo: Calculate adjacency information
                // Note that we don't actually use this information, so it's a low-priorty todo
                writer.Write(-1); writer.Write(-1); writer.Write(-1);
            }
        }

        void WriteVertexArrays(BinaryWriter writer, ref Header header)
        {
            VertexArray[] arrays =
            {
                VertexArray.FromType(VertexArrayType.Position),
                VertexArray.FromType(VertexArrayType.Normal),
                VertexArray.FromType(VertexArrayType.Tangent),
                VertexArray.FromType(VertexArrayType.TexCoord),
                VertexArray.FromType(VertexArrayType.Color),
                VertexArray.FromType(VertexArrayType.BlendIndexes),
                VertexArray.FromType(VertexArrayType.BlendWeights),
            };

            for (int i = 0; i < arrays.Length; i++)
            {
                var array    = arrays[i];
                array.Offset = (int)writer.BaseStream.Position;

                for (int v = 0; v < Vertices.Count; v++)
                {
                    switch (arrays[i].Type)
                    {
                    case VertexArrayType.TexCoord:
                        writer.Write(Vertices[v].TexCoord.x);
                        writer.Write(1f - Vertices[v].TexCoord.y);
                        break;
                    case VertexArrayType.Position:
                        writer.Write(CoordinateHelper.UnityToQuake(Vertices[v].Position));
                        break;
                    case VertexArrayType.Normal:
                        writer.Write(CoordinateHelper.UnityToQuake(Vertices[v].Normal));
                        break;
                    case VertexArrayType.Tangent:
                        writer.Write(CoordinateHelper.UnityToQuake(Vertices[v].Tangent));
                        break;
                    case VertexArrayType.BlendIndexes:
                        writer.Write((byte)Vertices[v].Weight.boneIndex0);
                        writer.Write((byte)Vertices[v].Weight.boneIndex1);
                        writer.Write((byte)Vertices[v].Weight.boneIndex2);
                        writer.Write((byte)Vertices[v].Weight.boneIndex3);
                        break;
                    case VertexArrayType.BlendWeights:
                        writer.Write((byte)(Vertices[v].Weight.weight0 * 255f));
                        writer.Write((byte)(Vertices[v].Weight.weight1 * 255f));
                        writer.Write((byte)(Vertices[v].Weight.weight2 * 255f));
                        writer.Write((byte)(Vertices[v].Weight.weight3 * 255f));
                        break;
                    case VertexArrayType.Color:
                        writer.Write(Vertices[v].Color.r);
                        writer.Write(Vertices[v].Color.g);
                        writer.Write(Vertices[v].Color.b);
                        writer.Write(Vertices[v].Color.a);
                        break;
                    }
                }

                arrays[i] = array;
            }

            header.VertexCount       = Vertices.Count;
            header.VertexArrayCount  = arrays.Length;
            header.VertexArrayOffset = (int)writer.BaseStream.Position;
            foreach (var array in arrays) array.Write(writer);
        }

        void WriteBones(BinaryWriter writer, TextTable text, ref Header header)
        {
            header.JointOffset = (int)writer.BaseStream.Position;
            header.JointCount  = Bones.Count;

            foreach (var bone in Bones)
            {
                int parent = Bones.IndexOf(bone.Parent);

                if (parent == -1 && bone.Parent != null)
                    throw new Exception("Bone has invalid parent.");

                writer.Write(text.GetOrAddIndex(bone.Name));
                writer.Write(parent);
                writer.Write(CoordinateHelper.UnityToQuake(bone.Translate));
                writer.Write(CoordinateHelper.UnityToQuake(bone.Rotate));
                writer.Write(CoordinateHelper.UnityToQuake(bone.Scale, isScale: true));
            }
        }

        struct KeyFrame
        {
            public Bone Bone;
            public Vector3 Translate;
            public Quaternion Rotate;
            public Vector3 Scale;
        }

        struct Pose
        {
            public int Parent;
            public int ChannelMask;

            public PoseChannels ChannelOffset;
            public PoseChannels ChannelScale;

            public PoseChannels Max;
            public PoseChannels Min;

            public void Write(BinaryWriter writer)
            {
                writer.Write(Parent);
                writer.Write(ChannelMask);

                for (int i = 0; i < 10; i++)
                    writer.Write(ChannelOffset[i]);

                for (int i = 0; i < 10; i++)
                    writer.Write(ChannelScale[i]);
            }
        }

        unsafe struct PoseChannels
        {
            fixed float channel[10];

            public float this[int index]
            {
                get
                {
                    fixed (float* channel = this.channel)
                        return channel[index];
                }

                set
                {
                    fixed (float* channel = this.channel)
                        channel[index] = value;
                }
            }
        }

        struct Header
        {
            public int Version;
            public int FileSize;
            public int Flags;

            public int TextCount, TextOffset;
            public int MeshCount, MeshOffset;
            public int VertexArrayCount, VertexCount, VertexArrayOffset;
            public int TriangleCount, TriangleOffset, AdjacencyOffset;
            public int JointCount, JointOffset;
            public int PoseCount, PoseOffset;
            public int AnimCount, AnimOffset;
            public int FrameCount, FrameChannelCount, FrameOffset, BoundsOffset;
            public int CommentCount, CommentOffset;
            public int ExtensionCount, ExtensionOffset;

            public void Write(BinaryWriter writer)
            {
                writer.Write(Version);
                writer.Write(FileSize);
                writer.Write(Flags);
                writer.Write(TextCount);
                writer.Write(TextOffset);
                writer.Write(MeshCount);
                writer.Write(MeshOffset);
                writer.Write(VertexArrayCount);
                writer.Write(VertexCount);
                writer.Write(VertexArrayOffset);
                writer.Write(TriangleCount);
                writer.Write(TriangleOffset);
                writer.Write(AdjacencyOffset);
                writer.Write(JointCount);
                writer.Write(JointOffset);
                writer.Write(PoseCount);
                writer.Write(PoseOffset);
                writer.Write(AnimCount);
                writer.Write(AnimOffset);
                writer.Write(FrameCount);
                writer.Write(FrameChannelCount);
                writer.Write(FrameOffset);
                writer.Write(BoundsOffset);
                writer.Write(CommentCount);
                writer.Write(CommentOffset);
                writer.Write(ExtensionCount);
                writer.Write(ExtensionOffset);
            }
        }
    }
}
