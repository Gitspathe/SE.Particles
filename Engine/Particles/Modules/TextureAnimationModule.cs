using SE.Core;
using System;
using System.Runtime.InteropServices;
using System.Security;
using static SE.Particles.ParticleMath;

namespace SE.Particles.Modules
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe class TextureAnimationModule : ParticleModule
    {
        public int SheetRows;
        public int SheetColumns;
        public float Speed;

        private LoopMode loopMode;

        public TextureAnimationModule()
        {
        }

        private void ApplyToEmitter()
        {
            if (Emitter == null)
                return;

            //Emitter.ParticleSize = new Int2(
            //    (int)Emitter.TextureSize.X / SheetColumns, 
            //    (int)Emitter.TextureSize.Y / SheetRows);
        }

        public void SetOverLifetime(int sheetRows, int sheetColumns)
        {
            SheetRows = sheetRows;
            SheetColumns = sheetColumns;
            ApplyToEmitter();
        }

        public override void OnInitialize()
        {
            base.OnInitialize();
            ApplyToEmitter();
        }

        public override void OnUpdate(float deltaTime, Particle* arrayPtr, int length)
        {
            Particle* tail = arrayPtr + length;
            int totalFrames = SheetRows * SheetColumns;
            int frameSize = (int)Emitter.Config.Texture.FullTextureSize.X / SheetRows;
            switch (loopMode) {
                case LoopMode.Life: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        int frame = (int)Between(0.0f, totalFrames, particle->TimeAlive / particle->InitialLife);
#if NETSTANDARD2_1
                        int frameX = (int)MathF.Floor(frame % SheetRows);
                        int frameY = (int)MathF.Floor(frame / SheetRows);
#else
                        int frameX = (int)Math.Floor((double)frame % SheetRows);
                        int frameY = (int)Math.Floor((double)frame / SheetRows);
#endif
                        particle->SourceRectangle = new Int4(
                             frameX * frameSize,
                             frameY * frameSize,
                             frameSize,
                             frameSize);
                    }
                }
                break;
                case LoopMode.Loop: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        // TODO:
                    }
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ParticleModule DeepCopy()
        {
            return new TextureAnimationModule {
                SheetRows = SheetRows,
                SheetColumns = SheetColumns,
                loopMode = loopMode
            };
        }

        public static TextureAnimationModule OverLifetime(int sheetRows, int sheetColumns)
        {
            TextureAnimationModule mod = new TextureAnimationModule();
            mod.SetOverLifetime(sheetRows, sheetColumns);
            return mod;
        }

        private enum LoopMode
        {
            Life,
            Loop
        }
    }
}
