using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SE.Particles
{
    public static class ParticleMath
    {
        public const float _PI = (float)Math.PI;
        public const float _2PI = (float)(Math.PI * 2.0);
        public const float _PI_OVER180 = (float)Math.PI / 180;
        public const float _180_OVER_PI = (float)(180 / Math.PI);

        public static Vector2 UpDirection => new Vector2(0.00000000f, -1.00000000f);
        public static Vector2 RightDirection => new Vector2(1.00000000f, 0.00000367f);
        public static Vector2 DownDirection => new Vector2(0.00000265f, 1.00000000f);
        public static Vector2 LeftDirection => new Vector2(-1.00000000f, -0.00000102f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Lerp(float value1, float value2, float amount)
            => value1 + (value2 - value1) * amount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Lerp(byte value1, byte value2, float amount)
            => (byte)(value1 + (value2 - value1) * amount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref float x, ref float y)
        {
            float tmpX = x;
            x = y;
            y = tmpX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap(ref byte x, ref byte y)
        {
            byte tmpX = x;
            x = y;
            y = tmpX;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Clamp(float val, float min, float max)
        {
            if (val < min)
                return min;
            if (val > max)
                return max;

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Clamp(byte val, byte min, byte max)
        {
            if (val < min)
                return min;
            if (val > max)
                return max;

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Between(float min, float max, float ratio)
            => (min + ((max - min) * ratio));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte Between(byte min, byte max, float ratio)
            => (byte)(min + ((max - min) * ratio));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetRatio(float min, float max, float val)
            => (val - min) / (max - min);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(float degrees)
            => _PI_OVER180 * degrees;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToDegrees(float radians)
            => radians * _180_OVER_PI;
    }
}
