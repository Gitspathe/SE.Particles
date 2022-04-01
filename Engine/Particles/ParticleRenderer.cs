using System;
using System.Numerics;
using SE.Core;
using Vector2 = System.Numerics.Vector2;
using System.Runtime.InteropServices;
using System.Buffers;

#if MONOGAME
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MGVector3 = Microsoft.Xna.Framework.Vector3;
#endif

namespace SE.Particles
{
    public abstract class ParticleRendererBase : IDisposable
    {
        protected internal Emitter Emitter { get; internal set; }

        internal void InitializeInternal(Emitter emitter)
        {
            Emitter = emitter;
            Initialize();
        }

        protected internal virtual void Initialize() { }
        protected internal virtual void OnParticleSizeChanged() { }
        protected internal virtual void OnEmitterUpdate() { }

        public abstract void Dispose();
    }

    public abstract class ParticleRenderer : ParticleRendererBase
    {
        protected internal abstract void Draw(Matrix4x4 cameraMatrix);

        public override void Dispose() { }
    }

#if MONOGAME
    public abstract class MGParticleRenderer : ParticleRendererBase
    {
        protected GraphicsDevice Graphics { get; } = ParticleEngine.GraphicsDevice;

        protected internal abstract void Draw(Matrix cameraMatrix);

        public override void Dispose() { }
    }

    public unsafe class InstancedParticleRenderer : MGParticleRenderer
    {
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private VertexBufferBinding vertexBufferBinding;

        private InstanceData[] instanceData;
        private DynamicVertexBuffer instanceBuffer;
        private VertexBufferBinding instanceBufferBinding;

        private Effect effect;
        private Matrix view, projection;

        public InstancedParticleRenderer()
        {
            effect = ParticleEngine.ParticleInstanceEffect;
        }

        protected internal override void OnParticleSizeChanged()
        {
            base.OnParticleSizeChanged();
            SetupVertexBuffer();
        }

        protected internal override void OnEmitterUpdate()
        {
            base.OnEmitterUpdate();
            UpdateBuffers();
        }

        protected internal override void Initialize()
        {
            Viewport viewPort = Graphics.Viewport;

            view = Matrix.CreateLookAt(new MGVector3(0, 0, 0), MGVector3.Forward, MGVector3.Up);
            projection = Matrix.CreateOrthographicOffCenter(0, viewPort.Width, viewPort.Height, 0, 0, -10);
            projection = view * projection;

            InitializeBuffers();
        }

        internal void UpdateBuffers()
        {
            Emitter emitter = Emitter;
            int i = 0;

            // copy to instance buffer.
            fixed (Particle* arrPtr = emitter.Particles) {
                Particle* tail = arrPtr + emitter.NumActive;
                for (Particle* p = arrPtr; p < tail; p++, i++) {
                    fixed (InstanceData* data = &instanceData[i]) {

                        data->TextureCoordOffset = new Vector2(p->SourceRectangle.X, p->SourceRectangle.Y) / emitter.Config.Texture.FullTextureSize;

                        // If we are running modern .net, use a fast MemoryCopy. Otherwise, manually copy over data.
#if NETSTANDARD2_0_OR_GREATER
                        Buffer.MemoryCopy(p, data, InstanceData._SIZE_IN_BYTES, Particle._RENDER_SIZE);
#else
                        data->InstanceScale = p->Scale;
                        data->InstanceRotation = p->SpriteRotation;
                        data->InstancePosition = p->Position;
                        data->InstanceColor = p->Color;
#endif
                    }
                }
            }
        }

        protected internal override void Draw(Matrix cameraMatrix)
        {
            Emitter emitter = Emitter;
            if (Emitter.NumActive == 0)
                return;

            // Setup various graphics device stuff.
            // TODO: See how SpriteBatch handles manually setting these!
            Graphics.BlendState = BlendState.NonPremultiplied;
            Graphics.DepthStencilState = DepthStencilState.None;
            Graphics.SamplerStates[0] = SamplerState.PointClamp;
            Graphics.RasterizerState = RasterizerState.CullCounterClockwise;

            // Update parameters. May only need to be set when one of these values actually changes, not every frame.
            effect.CurrentTechnique = effect.Techniques["ParticleInstancing"];
            effect.Parameters["World"].SetValue(cameraMatrix * projection);
            effect.Parameters["ParticleTexture"].SetValue(emitter.Config.Texture.Texture);
            instanceBuffer.SetData(instanceData, 0, emitter.NumActive);

            // set buffer bindings to the device
            Graphics.Indices = indexBuffer;
            Graphics.SetVertexBuffers(vertexBufferBinding, instanceBufferBinding);

            // Set the shader technique pass and then Draw
            effect.CurrentTechnique.Passes[0].Apply();
            Graphics.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, emitter.NumActive);
        }

        internal void SetupVertexBuffer()
        {
            Emitter emitter = Emitter;

            // Create a single quad origin is dead center of the quad it could be top left instead.
            Int2 particleSize = new Int2((int)emitter.Config.Texture.Size.X, (int)emitter.Config.Texture.Size.Y);

            Vector2 sizeFloat = new Vector2(
                emitter.Config.Texture.Size.X / emitter.Config.Texture.FullTextureSize.X,
                emitter.Config.Texture.Size.Y / emitter.Config.Texture.FullTextureSize.Y);

            float halfWidth = particleSize.X / 2.0f;
            float halfHeight = particleSize.Y / 2.0f;
            float left = -halfWidth; float right = halfWidth;
            float top = -halfHeight; float bottom = halfHeight;

            VertexPositionTexture[] vertices = new VertexPositionTexture[4];
            vertices[0] = new VertexPositionTexture { Position = new Vector2(left, top), TextureCoordinate = new Vector2(0f, 0f) };
            vertices[1] = new VertexPositionTexture { Position = new Vector2(left, bottom), TextureCoordinate = new Vector2(0f, sizeFloat.Y) };
            vertices[2] = new VertexPositionTexture { Position = new Vector2(right, bottom), TextureCoordinate = new Vector2(sizeFloat.X, sizeFloat.Y) };
            vertices[3] = new VertexPositionTexture { Position = new Vector2(right, top), TextureCoordinate = new Vector2(sizeFloat.X, 0f) };

            vertexBuffer.SetData(vertices);
        }

        private void InitializeBuffers()
        {
            Emitter emitter = Emitter;
            int particlesLength = emitter.Particles.Length;

            indexBuffer = new IndexBuffer(Graphics, typeof(short), 6, BufferUsage.WriteOnly);
            vertexBuffer = new VertexBuffer(Graphics, VertexPositionTexture.VertexDeclaration, 4, BufferUsage.WriteOnly);
            instanceBuffer = new DynamicVertexBuffer(Graphics, InstanceData.VertexDeclaration, particlesLength, BufferUsage.WriteOnly);

            SetupVertexBuffer();

            // set up the indice stuff.
            short[] indices = new short[6];
            if (Graphics.RasterizerState == RasterizerState.CullClockwise) {
                indices[0] = 0; indices[1] = 1; indices[2] = 2;
                indices[3] = 2; indices[4] = 3; indices[5] = 0;
            } else {
                indices[0] = 0; indices[1] = 2; indices[2] = 1;
                indices[3] = 2; indices[4] = 0; indices[5] = 3;
            }

            // set up the instance stuff
            instanceData = ArrayPool<InstanceData>.Shared.Rent(particlesLength);
            for (int i = 0; i < particlesLength; i++) {
                instanceData[i].InstanceColor = ParticleColor.Black;
                instanceData[i].InstancePosition = new Vector2(0, 0);
            }

            // create buffers and set the data to them.
            indexBuffer.SetData(indices);
            instanceBuffer.SetData(instanceData);

            // create the bindings.
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
            instanceBufferBinding = new VertexBufferBinding(instanceBuffer, 0, 1);
        }

        public override void Dispose()
        {
            instanceBuffer?.Dispose();
            vertexBuffer?.Dispose();
            indexBuffer?.Dispose();
            ArrayPool<InstanceData>.Shared.Return(instanceData);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceData : IVertexType
    {
        public Vector2 InstancePosition;
        public Vector2 InstanceScale;
        public float InstanceRotation;
        public ParticleColor InstanceColor;
        public Vector2 TextureCoordOffset;

        public static readonly VertexDeclaration VertexDeclaration;
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public const long _SIZE_IN_BYTES = 32;

        static InstanceData()
        {
            var elements = new[] {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 1),
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.Position, 2),
                new VertexElement(16, VertexElementFormat.Single, VertexElementUsage.Position, 3),
                new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 1),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
            };
            VertexDeclaration = new VertexDeclaration(elements);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionTexture : IVertexType
    {
        public Vector2 Position;
        public Vector2 TextureCoordinate;
        public static readonly VertexDeclaration VertexDeclaration;
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        static VertexPositionTexture()
        {
            var elements = new[] {
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            };
            VertexDeclaration = new VertexDeclaration(elements);
        }
    }

    // TODO: SpriteBatchRenderer.
}
#endif
