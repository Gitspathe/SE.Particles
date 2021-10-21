using System;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;

namespace SE.Particles
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Particle
    {
        public Vector2 Position;     // X, Y position.
        public Vector2 Scale;        // X, Y scale.
        public float SpriteRotation; // Sprite rotation.
        public ParticleColor Color;  // H, S, L, A.

        public int ID;               // Used for identification (i.e random).
        public Vector2 Direction;    // Direction the particle travels in.
        public float Mass;           // Used for repel and attract type functionality.
        public float Speed;          // Speed. Combined with Direction to get the velocity.
        public float InitialLife;    // Activated life.
        public float TimeAlive;      // Time active.
        public float layerDepth;     // Draw order.
        public Int4 SourceRectangle; // Texture source rectangle.

        public static readonly int SizeInBytes = Marshal.SizeOf(typeof(Particle));

        public const long _RENDER_SIZE = 24;

        public static Particle Default
            => new Particle(0, Vector2.Zero, Vector2.One, ParticleColor.Black, 0f, 1.0f);

        public Particle(int id, Vector2 position, Vector2 scale, ParticleColor color, float spriteRotation, float timeAlive)
        {
            ID = id;
            Position = position;
            Scale = scale;
            Direction = Vector2.Zero;
            Color = color;
            Mass = 0.0f;
            Speed = 0.0f;
            SpriteRotation = spriteRotation;
            TimeAlive = timeAlive;
            InitialLife = timeAlive;
            layerDepth = 0.0f;
            SourceRectangle = new Int4(0, 0, 128, 128);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int4 : IEquatable<Int4>
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public Int4(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Equals(Int4 other)
            => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        public override bool Equals(object obj)
            => obj is Int4 other && Equals(other);
        public override int GetHashCode()
            => X ^ Y ^ Width ^ Height;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int2 : IEquatable<Int2>
    {
        public int X;
        public int Y;

        public Int2(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(Int2 other)
            => X == other.X && Y == other.Y;
        public override bool Equals(object obj)
            => obj is Int2 other && Equals(other);
        public override int GetHashCode()
            => X ^ Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleColor : IEquatable<ParticleColor>
    {
        // Alpha, Lightness, Saturation, Hue
        private uint packedValue;

        private byte AlphaByte {
            get {
                unchecked {
                    return (byte)(packedValue >> 24);
                }
            }
            set => packedValue = (packedValue & 0x00ffffff) | ((uint)value << 24);
        }

        private byte LightnessByte {
            get {
                unchecked {
                    return (byte)(packedValue >> 16);
                }
            }
            set => packedValue = (packedValue & 0xff00ffff) | ((uint)value << 16);
        }

        private byte SaturationByte {
            get {
                unchecked {
                    return (byte)(packedValue >> 8);
                }
            }
            set => packedValue = (packedValue & 0xffff00ff) | ((uint)value << 8);
        }

        private byte HueByte {
            get {
                unchecked {
                    return (byte)packedValue;
                }
            }
            set => packedValue = (packedValue & 0xffffff00) | value;
        }

        public float Alpha {
            get => AlphaByte / 255.0f;
            set => AlphaByte = ParticleMath.Clamp((byte)(value * 255), byte.MinValue, byte.MaxValue);
        }

        public float Lightness {
            get => (LightnessByte / 255.0f) * 100.0f;
            set => LightnessByte = ParticleMath.Clamp((byte)((value / 100.0f) * 255.0f), byte.MinValue, byte.MaxValue);
        }

        public float Saturation {
            get => (SaturationByte / 255.0f) * 100.0f;
            set => SaturationByte = ParticleMath.Clamp((byte)((value / 100.0f) * 255.0f), byte.MinValue, byte.MaxValue);
        }

        public float Hue {
            get => (HueByte / 255.0f) * 360.0f;
            set => HueByte = ParticleMath.Clamp((byte)((value / 360.0f) * 255.0f), byte.MinValue, byte.MaxValue);
        }

        /// <summary>
        /// Sets HSL color.
        /// </summary>
        /// <param name="h">Hue, from 0 - 360.</param>
        /// <param name="s">Saturation, from 0 - 100</param>
        /// <param name="l">Lightness, from 0 - 100</param>
        /// <param name="a">Alpha, from 0 - 1</param>
        public ParticleColor(float h, float s, float l, float a)
        {
            packedValue = 0;

            HueByte = h == 0 ? (byte)0 : (byte)ParticleMath.Clamp((h / 360.0f) * 255, byte.MinValue, byte.MaxValue);
            SaturationByte = s == 0 ? (byte)0 : (byte)ParticleMath.Clamp((s / 100.0f) * 255, byte.MinValue, byte.MaxValue);
            LightnessByte = l == 0 ? (byte)0 : (byte)ParticleMath.Clamp((l / 100.0f) * 255, byte.MinValue, byte.MaxValue);
            AlphaByte = a == 0 ? (byte)0 : (byte)ParticleMath.Clamp((a / 1.0f) * 255, byte.MinValue, byte.MaxValue); ;
        }

        public static bool operator ==(ParticleColor a, ParticleColor b)
        {
            return (a.AlphaByte == b.AlphaByte &&
                    a.HueByte == b.HueByte &&
                    a.SaturationByte == b.SaturationByte &&
                    a.LightnessByte == b.LightnessByte);
        }

        public static bool operator !=(ParticleColor a, ParticleColor b)
        {
            return !(a == b);
        }

        public override int GetHashCode()
        {
            return (int)packedValue;
        }

        public override bool Equals(object obj)
        {
            return obj is ParticleColor color && Equals(color);
        }

        public bool Equals(ParticleColor other)
        {
            return packedValue == other.packedValue;
        }

        public static ParticleColor Lerp(ParticleColor value1, ParticleColor value2, float amount)
        {
            amount = ParticleMath.Clamp(amount, 0, 1);
            return new ParticleColor(
                ParticleMath.Lerp(value1.HueByte, value2.HueByte, amount),
                ParticleMath.Lerp(value1.SaturationByte, value2.SaturationByte, amount),
                ParticleMath.Lerp(value1.LightnessByte, value2.LightnessByte, amount),
                ParticleMath.Lerp(value1.AlphaByte, value2.AlphaByte, amount));
        }

        public static ParticleColor Black => new ParticleColor(0, 0, 0, 0);
    }
}
