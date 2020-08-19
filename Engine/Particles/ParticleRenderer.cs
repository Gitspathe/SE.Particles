#if MONOGAME
using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SE.Core;
using Vector3 = Microsoft.Xna.Framework.Vector3;

using Vector2 = System.Numerics.Vector2;
using MGVector2 = Microsoft.Xna.Framework.Vector2;
using System.Runtime.InteropServices;

namespace SE.Particles
{
    // TODO: Needs some massive fucking improvement.
    // TODO: Support texture source rectangles: This is partially implemented. Need to clean it up!
    public unsafe class ParticleRenderer : IDisposable
    {
        private Game game => ParticleEngine.Game;
        private GraphicsDeviceManager gdm => ParticleEngine.GraphicsDeviceManager;
        private GraphicsDevice gd => ParticleEngine.Game.GraphicsDevice;

        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private VertexBufferBinding vertexBufferBinding;

        private InstanceData[] instanceData;
        private VertexBuffer instanceBuffer;
        private VertexBufferBinding instanceBufferBinding;

        private Effect effect;
        private Matrix world, view, projection;
        private Vector2 particleSize;
        private Emitter emitter;

        public ParticleRenderer(Emitter emitter)
        {
            this.emitter = emitter;
            effect = ParticleEngine.particleInstanceEffect;
            Initialize();
        }

        // TODO.
        public void Draw(Vector2 cameraPosition)
        {
            // copy to instance buffer.
            fixed(Particle* arrPtr = emitter.Particles) {
                Particle* copy = arrPtr;
                Parallel.For(0, emitter.NumActive, (i) => {
                    Particle* p = copy + i;

                    // TODO: There's a better way to do this lol.
                    Vector2 texSize = emitter.TextureSize;
                    Vector2 offset = new Vector2(p->SourceRectangle.X, p->SourceRectangle.Y);


                    instanceData[i].TextureCoordOffset = offset / texSize;

                    instanceData[i].InstancePosition = new Vector3(
                        p->Position.X - cameraPosition.X, 
                        p->Position.Y - cameraPosition.Y, 
                        p->layerDepth);
                    
                    instanceData[i].InstanceColor = new Color(p->Color.X / 360, p->Color.Y, p->Color.Z, p->Color.W);
                });
            }

            // Update parameters. May only need to be set when one of these values actually changes, not every frame.
            effect.CurrentTechnique = effect.Techniques["ParticleInstancing"];
            effect.Parameters["World"].SetValue(ParticleEngine.WorldMatrix);
            effect.Parameters["ParticleTexture"].SetValue(emitter.Texture);
            instanceBuffer.SetData(instanceData);

            // set buffer bindings to the device
            gd.SetVertexBuffers(vertexBufferBinding, instanceBufferBinding);
            gd.Indices = indexBuffer;

            // Set the shader technique pass and then Draw
            effect.CurrentTechnique.Passes[0].Apply();
            gd.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, emitter.NumActive);
        }

        internal void Initialize()
        {
            bool useSpriteBatchProjection = true;

            // TODO: Temporary.
            particleSize = new Vector2(32, 32);

            var viewPort = game.GraphicsDevice.Viewport;
            float aspect = viewPort.Width / viewPort.Height;

            world = Matrix.Identity;
            view = Matrix.CreateLookAt(new Vector3(0, 0, 0), Vector3.Forward, Vector3.Up);
            projection = Matrix.CreateOrthographicOffCenter(0, viewPort.Width, viewPort.Height, 0, 0, -10);

            effect.Parameters["View"].SetValue(view);
            effect.Parameters["Projection"].SetValue(projection);

            InitializeBuffers();
        }

        private void InitializeBuffers()
        {
            // Create a single quad origin is dead center of the quad it could be top left instead.
            float halfWidth = particleSize.X / 2;
            float halfHeight = particleSize.Y / 2;
            float Left = -halfWidth; float Right = halfWidth;
            float Top = -halfHeight; float Bottom = halfHeight;

            // TODO: Need to correctly set up spritesheet texture coords here. Needs to be sized for the original portion.
            VertexPositionTexture[] vertices = new VertexPositionTexture[4];
            vertices[0] = new VertexPositionTexture() { Position = new Vector3(Left, Top, 0f), TextureCoordinate = new Vector2(0f, 0f) };
            vertices[1] = new VertexPositionTexture() { Position = new Vector3(Left, Bottom, 0f), TextureCoordinate = new Vector2(0f, 0.2f) };
            vertices[2] = new VertexPositionTexture() { Position = new Vector3(Right, Bottom, 0f), TextureCoordinate = new Vector2(0.2f, 0.2f) };
            vertices[3] = new VertexPositionTexture() { Position = new Vector3(Right, Top, 0f), TextureCoordinate = new Vector2(0.2f, 0f) };

            // set up the indice stuff.
            int[] indices = new int[6];
            if (gd.RasterizerState == RasterizerState.CullClockwise) {
                indices[0] = 0; indices[1] = 1; indices[2] = 2;
                indices[3] = 2; indices[4] = 3; indices[5] = 0;
            } else {
                indices[0] = 0; indices[1] = 2; indices[2] = 1;
                indices[3] = 2; indices[4] = 0; indices[5] = 3;
            }

            // set up the instance stuff

            int particlesLength = emitter.Particles.Length;
            instanceData = new InstanceData[particlesLength];
            for (int i = 0; i < particlesLength; ++i) {
                instanceData[i].InstanceColor = Color.White;
                instanceData[i].InstancePosition = new Vector3(0, 0, 0);
            }

            // create buffers and set the data to them.
            indexBuffer = new IndexBuffer(gd, typeof(int), 6, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
            vertexBuffer = new VertexBuffer(gd, VertexPositionTexture.VertexDeclaration, 4, BufferUsage.WriteOnly);
            vertexBuffer.SetData(vertices);
            instanceBuffer = new VertexBuffer(gd, InstanceData.VertexDeclaration, particlesLength, BufferUsage.WriteOnly);
            instanceBuffer.SetData(instanceData);

            // create the bindings.
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
            instanceBufferBinding = new VertexBufferBinding(instanceBuffer, 0, 1);
        }

        public void Dispose()
        {
            instanceBuffer?.Dispose();
            effect?.Dispose();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceData : IVertexType
    {
        public Vector3 InstancePosition;
        public Color InstanceColor;
        public Vector2 TextureCoordOffset;
        // TODO: Instance scale.

        public static readonly VertexDeclaration VertexDeclaration;
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        static InstanceData()
        {
            var elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 1), // Note the 1 not a 0 used by the VertexPositionTexture UsageIndex.
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 1),
                new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1)
            };
            VertexDeclaration = new VertexDeclaration(elements);
        }
    }

    public struct VertexPositionTexture : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;
        public static readonly VertexDeclaration VertexDeclaration;
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        static VertexPositionTexture()
        {
            var elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            };
            VertexDeclaration = new VertexDeclaration(elements);
        }
    }
}
#endif