using System;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace SE.Particles
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Particle
    {
        public Vector2 Position;
        public Vector2 Scale;
        public int ID;               // Used for identification (i.e random).

        // TODO: Change from Vector2 to float? (Could fully update particle transform in shader.)
        public Vector2 Direction;    // Direction the particle travels in.
        public Vector4 Color;        // H, S, L, A.
        public float Mass;           // Used for repel and attract type functionality.
        public float Speed;
        public float SpriteRotation; // Sprite rotation.
        public float InitialLife;
        public float TimeAlive;
        public float layerDepth;     // Draw order.
        public Int4 SourceRectangle;

        public static readonly int SizeInBytes = Marshal.SizeOf(typeof(Particle));

        public static Particle Default 
            => new Particle(0, Vector2.Zero, Vector2.One, Vector4.Zero, 0f, 1.0f);

        public Particle(int ID, Vector2 position, Vector2 scale, Vector4 color, float spriteRotation, float timeAlive)
        {
            this.ID = ID;
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
}
