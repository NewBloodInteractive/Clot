using System;

namespace NewBlood.Clot
{
    [Flags]
    public enum AnimationFlags
    {
        // These currently match the flags used by the IQM format.
        // If we add new flags here, we'll need to tweak the builder
        // to support them.

        None = 0,
        Loop = 1 << 0,
    }

    public struct AnimationRange
    {
        public string Name { get; set; }

        public AnimationFlags Flags { get; set; }

        public float FrameRate { get; set; }

        public int FrameIndex { get; set; }

        public int FrameCount { get; set; }
    }
}
