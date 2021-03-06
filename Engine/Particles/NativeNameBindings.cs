﻿using System.Numerics;
using System.Runtime.InteropServices;

namespace SE.Particles
{
    internal struct NativeCurve { /* For naming purposes. */ }
    internal struct NativeCurve2 { /* For naming purposes. */ }
    internal struct NativeCurve3 { /* For naming purposes. */ }
    internal struct NativeCurve4 { /* For naming purposes. */ }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeVector2
    {
        public float x, y;

        public NativeVector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public NativeVector2(Vector2 vec)
        {
            this.x = vec.X;
            this.y = vec.Y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeVector4
    {
        public float x, y, z, w;

        public NativeVector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public NativeVector4(Vector4 vec)
        {
            this.x = vec.X;
            this.y = vec.Y;
            this.z = vec.Z;
            this.w = vec.W;
        }
    }

    public struct Module { /* Used for naming only! */ }
    public struct Submodule { /* Used for naming only! */ }
}
