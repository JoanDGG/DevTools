namespace PixelPlay.Foundation.Input
{
    /// <summary>Unity-independent two-dimensional input value.</summary>
    public readonly struct InputVector2
    {
        /// <summary>Creates a two-dimensional value.</summary>
        public InputVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Gets the horizontal component.</summary>
        public float X { get; }
        /// <summary>Gets the vertical component.</summary>
        public float Y { get; }
    }

    /// <summary>Unity-independent three-dimensional input value.</summary>
    public readonly struct InputVector3
    {
        /// <summary>Creates a three-dimensional value.</summary>
        public InputVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Gets the X component.</summary>
        public float X { get; }
        /// <summary>Gets the Y component.</summary>
        public float Y { get; }
        /// <summary>Gets the Z component.</summary>
        public float Z { get; }
    }

    /// <summary>Unity-independent quaternion input value.</summary>
    public readonly struct InputQuaternion
    {
        /// <summary>Creates a quaternion value using X, Y, Z, and W components.</summary>
        public InputQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary>Gets the X component.</summary>
        public float X { get; }
        /// <summary>Gets the Y component.</summary>
        public float Y { get; }
        /// <summary>Gets the Z component.</summary>
        public float Z { get; }
        /// <summary>Gets the W component.</summary>
        public float W { get; }
    }
}
