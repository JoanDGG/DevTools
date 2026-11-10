namespace PixelPlay.Foundation.Input
{
    /// <summary>Unity-independent two-dimensional input value.</summary>
    public readonly struct InputVector2
    {
        public InputVector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }
        public float Y { get; }
    }

    /// <summary>Unity-independent three-dimensional input value.</summary>
    public readonly struct InputVector3
    {
        public InputVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }

    /// <summary>Unity-independent quaternion input value.</summary>
    public readonly struct InputQuaternion
    {
        public InputQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }
    }
}
