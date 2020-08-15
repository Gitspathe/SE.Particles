using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace SE.Particles
{

    // TODO: Support partial GPU instancing - https://community.monogame.net/t/how-to-particle-system-a-link-to-mrgraks-small-github-cpu-particle-example/12992/8
    // TODO: Modules should still probably work on the CPU, maybe.

    [StructLayout(LayoutKind.Sequential)]
    public struct Particle
    {
        public Vector2 Position;
        public Vector2 Scale;
        public Vector2 Direction;    // Direction the particle travels in.
        public Vector4 Color;        // H, S, L, A.
        public float Mass;           // Used for repel and attract type functionality.
        public float Speed;
        public float SpriteRotation; // Sprite rotation.
        public float InitialLife;
        public float TimeAlive;
        public float layerDepth;     // Draw order.
        public Int4 SourceRectangle; // Texture source rectangle. X, Y, Width, Height.

        public static readonly int SizeInBytes = Marshal.SizeOf(typeof(Particle));

        public static Particle Default 
            => new Particle(Vector2.Zero, Vector2.One, Vector4.Zero, 0f, 1.0f);

        public Particle(Vector2 position, Vector2 scale, Vector4 color, float spriteRotation, float timeAlive)
        { 
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
            SourceRectangle = new Int4(0, 0, 1, 1);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int4
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
    }
}
