﻿#if MONOGAME
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
using SE.Utility;
using System.Buffers;

namespace SE.Particles
{
    // TODO: Needs some massive fucking improvement.
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
        private Emitter emitter;

        public ParticleRenderer(Emitter emitter)
        {
            this.emitter = emitter;
            effect = ParticleEngine.particleInstanceEffect;
            Initialize();
        }

        internal void UpdateBuffers()
        {
            // copy to instance buffer.
            fixed(Particle* arrPtr = emitter.Particles) {
                Particle* tail = arrPtr + emitter.NumActive;
                int i = 0;
                for (Particle* p = arrPtr; p < tail; p++, i++) {
                    Vector2 offset = new Vector2(p->SourceRectangle.X, p->SourceRectangle.Y);
                    instanceData[i].InstanceScale = p->Scale;
                    instanceData[i].InstanceRotation = p->SpriteRotation;
                    instanceData[i].TextureCoordOffset = offset / emitter.TextureSize;
                    instanceData[i].InstanceColor = new Color(p->Color.X / 360, p->Color.Y, p->Color.Z, p->Color.W);
                }
            }
        }

        public void Draw(Vector2 cameraPosition)
        {
            // Update positions.
            fixed(Particle* arrPtr = emitter.Particles) {
                Particle* copy = arrPtr;
                QuickParallel.For(0, emitter.NumActive, (i) => {
                    Particle* p = copy + i;
                    instanceData[i].InstancePosition = new Vector3(
                        p->Position.X - cameraPosition.X, 
                        p->Position.Y - cameraPosition.Y, 
                        p->layerDepth);
                });
            }

            // Blend state.
            gd.BlendState = BlendState.Additive;

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
            Viewport viewPort = game.GraphicsDevice.Viewport;

            world = Matrix.Identity;
            view = Matrix.CreateLookAt(new Vector3(0, 0, 0), Vector3.Forward, Vector3.Up);
            projection = Matrix.CreateOrthographicOffCenter(0, viewPort.Width, viewPort.Height, 0, 0, -10);

            effect.Parameters["View"].SetValue(view);
            effect.Parameters["Projection"].SetValue(projection);

            InitializeBuffers();
        }

        internal void SetupVertexBuffer()
        {
            // Create a single quad origin is dead center of the quad it could be top left instead.
            Int2 particleSize = emitter.ParticleSize;

            Vector2 sizeFloat = new Vector2(
                emitter.ParticleSize.X / emitter.TextureSize.X,
                emitter.ParticleSize.Y / emitter.TextureSize.Y);

            float halfWidth = particleSize.X / 2.0f;
            float halfHeight = particleSize.Y / 2.0f;
            float left = -halfWidth; float right = halfWidth;
            float top = -halfHeight; float bottom = halfHeight;

            VertexPositionTexture[] vertices = new VertexPositionTexture[4];
            vertices[0] = new VertexPositionTexture() { Position = new Vector3(left, top, 0f), TextureCoordinate = new Vector2(0f, 0f) };
            vertices[1] = new VertexPositionTexture() { Position = new Vector3(left, bottom, 0f), TextureCoordinate = new Vector2(0f, sizeFloat.Y) };
            vertices[2] = new VertexPositionTexture() { Position = new Vector3(right, bottom, 0f), TextureCoordinate = new Vector2(sizeFloat.X, sizeFloat.Y) };
            vertices[3] = new VertexPositionTexture() { Position = new Vector3(right, top, 0f), TextureCoordinate = new Vector2(sizeFloat.X, 0f) };

            vertexBuffer.SetData(vertices);
        }

        private void InitializeBuffers()
        {
            int particlesLength = emitter.Particles.Length;

            indexBuffer = new IndexBuffer(gd, typeof(int), 6, BufferUsage.WriteOnly);
            vertexBuffer = new VertexBuffer(gd, VertexPositionTexture.VertexDeclaration, 4, BufferUsage.WriteOnly);
            instanceBuffer = new VertexBuffer(gd, InstanceData.VertexDeclaration, particlesLength, BufferUsage.WriteOnly);

            SetupVertexBuffer();

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
            instanceData = ArrayPool<InstanceData>.Shared.Rent(particlesLength);
            for (int i = 0; i < particlesLength; ++i) {
                instanceData[i].InstanceColor = Color.White;
                instanceData[i].InstancePosition = new Vector3(0, 0, 0);
            }

            // create buffers and set the data to them.
            indexBuffer.SetData(indices);
            instanceBuffer.SetData(instanceData);

            // create the bindings.
            vertexBufferBinding = new VertexBufferBinding(vertexBuffer);
            instanceBufferBinding = new VertexBufferBinding(instanceBuffer, 0, 1);
        }

        public void Dispose()
        {
            instanceBuffer?.Dispose();
            effect?.Dispose();
            ArrayPool<InstanceData>.Shared.Return(instanceData);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InstanceData : IVertexType
    {
        public Vector3 InstancePosition;
        public Color InstanceColor;
        public Vector2 TextureCoordOffset;
        public Vector2 InstanceScale;
        public float InstanceRotation;

        public static readonly VertexDeclaration VertexDeclaration;
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        static InstanceData()
        {
            var elements = new VertexElement[] {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 1), // Note the 1 not a 0 used by the VertexPositionTexture UsageIndex.
                new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 1),
                new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.Position, 2),
                new VertexElement(32, VertexElementFormat.Single, VertexElementUsage.Position, 3)
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