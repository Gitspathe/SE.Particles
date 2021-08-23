using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;
using SE.Core;
using SE.Engine.Utility;
using SE.Utility;
using Random = SE.Utility.Random;
using static SE.Particles.ParticleMath;

namespace SE.Particles.Modules
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe class AlphaModule : NativeParticleModule
    {
        private float[] startAlphas;
        private float[] randEndAlphas;

        private Transition transitionType;
        private float end1;
        private float end2;
        private Curve curve;

        private bool IsRandom => transitionType == Transition.RandomLerp;

        protected AlphaModule()
        {
            SubmodulePtr = nativeModule_AlphaModule_Ctor();
        }

        public void SetLerp(float end)
        {
            end1 = end;
            transitionType = Transition.Lerp;

            nativeModule_AlphaModule_SetLerp(SubmodulePtr, end);
        }

        public void SetCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = Transition.Curve;

            nativeModule_AlphaModule_SetCurve(SubmodulePtr, NativeUtil.CopyCurveToNativeCurve(curve));
        }

        public void SetRandomLerp(float min, float max)
        {
            if (min > max)
                Swap(ref min, ref max);

            end1 = min;
            end2 = max;
            transitionType = Transition.RandomLerp;
            RegenerateRandom();

            nativeModule_AlphaModule_SetRandomLerp(SubmodulePtr, min, max);
        }

        public override ParticleModule DeepCopy() 
            => new AlphaModule {
                transitionType = transitionType,
                end1 = end1,
                end2 = end2,
                curve = curve.Clone()
            };

        public override void OnInitialize()
        {
            startAlphas = new float[Emitter.ParticlesLength];
            RegenerateRandom();
        }

        private void RegenerateRandom()
        {
            if (!IsRandom || Emitter == null) 
                return;

            randEndAlphas = new float[Emitter.ParticlesLength];
        }

        public override void OnParticlesActivated(Span<int> particlesIndex)
        {
            if (ParticleEngine.NativeEnabled) {
                return;
            }

            fixed (Particle* particleArr = Emitter.Particles) {
                for (int i = 0; i < particlesIndex.Length; i++) {
                    Particle* particle = &particleArr[particlesIndex[i]];
                    startAlphas[particle->ID] = particle->Color.W;
                    if(!IsRandom)
                        continue;

                    randEndAlphas[particle->ID] = Between(end1, end2, Random.Next(0.0f, 1.0f));
                }
            }
        }

        public override void OnUpdate(float deltaTime, Particle* arrayPtr, int length)
        {
            if (ParticleEngine.NativeEnabled) {
                return;
            }

            Particle* tail = arrayPtr + length;
            int i = 0;

            switch (transitionType) {
                case Transition.Lerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++, i++) {
                        particle->Color = new Vector4(
                            particle->Color.X, 
                            particle->Color.Y, 
                            particle->Color.Z, 
                            ParticleMath.Lerp(startAlphas[particle->ID], end1, particle->TimeAlive / particle->InitialLife));
                    }
                } break;
                case Transition.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float lifeRatio = particle->TimeAlive / particle->InitialLife;
                        particle->Color = new Vector4(
                            particle->Color.X,
                            particle->Color.Y,
                            particle->Color.Z,
                            curve.Evaluate(lifeRatio));
                    }
                } break;
                case Transition.RandomLerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++, i++) {
                        particle->Color = new Vector4(
                            particle->Color.X,
                            particle->Color.Y,
                            particle->Color.Z,
                            ParticleMath.Lerp(startAlphas[particle->ID], randEndAlphas[particle->ID], particle->TimeAlive / particle->InitialLife));
                    }
                } break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static AlphaModule Lerp(float end)
        {
            AlphaModule module = new AlphaModule();
            module.SetLerp(end);
            return module;
        }

        public static AlphaModule Curve(Curve curve)
        {
            AlphaModule module = new AlphaModule();
            module.SetCurve(curve);
            return module;
        }

        public static AlphaModule RandomLerp(float min, float max)
        {
            AlphaModule module = new AlphaModule();
            module.SetRandomLerp(min, max);
            return module;
        }

        private enum Transition
        {
            Lerp,
            Curve,
            RandomLerp
        }

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern Submodule* nativeModule_AlphaModule_Ctor();
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_AlphaModule_SetNone(Submodule* modulePtr);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_AlphaModule_SetLerp(Submodule* modulePtr, float end);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_AlphaModule_SetRandomLerp(Submodule* modulePtr, float min, float max);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_AlphaModule_SetCurve(Submodule* modulePtr, NativeCurve* curvePtr);
    }
}
