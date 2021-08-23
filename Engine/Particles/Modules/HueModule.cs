using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using SE.Core;
using SE.Engine.Utility;
using SE.Particles.AreaModules;
using SE.Utility;
using Random = SE.Utility.Random;
using static SE.Particles.ParticleMath;

namespace SE.Particles.Modules
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe class HueModule : NativeParticleModule
    {
        private float[] startHues;
        private float[] randEndHues;

        private Transition transitionType;
        private float end1;
        private float end2;
        private Curve curve;

        private bool IsRandom => transitionType == Transition.RandomLerp;

        protected HueModule()
        {
            SubmodulePtr = nativeModule_HueModule_Ctor();
        }

        public void SetLerp(float end)
        {
            end1 = end;
            transitionType = Transition.Lerp;

            nativeModule_HueModule_SetLerp(SubmodulePtr, end);
        }

        public void SetCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = Transition.Curve;

            nativeModule_HueModule_SetCurve(SubmodulePtr, NativeUtil.CopyCurveToNativeCurve(curve));
        }

        public void SetRandomLerp(float min, float max)
        {
            if (min > max)
                Swap(ref min, ref max);

            end1 = min;
            end2 = max;
            transitionType = Transition.RandomLerp;

            nativeModule_HueModule_SetRandomLerp(SubmodulePtr, min, max);
        }

        public override ParticleModule DeepCopy() 
            => new HueModule {
                transitionType = transitionType,
                end1 = end1,
                end2 = end2,
                curve = curve.Clone()
            };

        public override void OnInitialize()
        {
            startHues = new float[Emitter.ParticlesLength];
            RegenerateRandom();
        }

        private void RegenerateRandom()
        {
            if (!IsRandom || Emitter == null) 
                return;

            randEndHues = new float[Emitter.ParticlesLength];
        }

        public override void OnParticlesActivated(Span<int> particlesIndex)
        {
            if (ParticleEngine.NativeEnabled) {
                return;
            }

            fixed (Particle* particleArr = Emitter.Particles) {
                for (int i = 0; i < particlesIndex.Length; i++) {
                    Particle* particle = &particleArr[particlesIndex[i]];
                    startHues[particle->ID] = particle->Color.X;
                    if (!IsRandom)
                        continue;

                    randEndHues[particle->ID] = Between(end1, end2, Random.Next(0.0f, 1.0f));
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
                            ParticleMath.Lerp(startHues[particle->ID], end1, particle->TimeAlive / particle->InitialLife), 
                            particle->Color.Y, 
                            particle->Color.Z,
                            particle->Color.W);
                    }
                } break;
                case Transition.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float lifeRatio = particle->TimeAlive / particle->InitialLife;
                        particle->Color = new Vector4(
                            curve.Evaluate(lifeRatio),
                            particle->Color.Y,
                            particle->Color.Z,
                            particle->Color.W);
                    }
                } break;
                case Transition.RandomLerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++, i++) {
                        particle->Color = new Vector4(
                            ParticleMath.Lerp(startHues[particle->ID], randEndHues[particle->ID], particle->TimeAlive / particle->InitialLife),
                            particle->Color.Y,
                            particle->Color.Z,
                            particle->Color.W);
                    }
                } break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static HueModule Lerp(float end)
        {
            HueModule module = new HueModule();
            module.SetLerp(end);
            return module;
        }

        public static HueModule Curve(Curve curve)
        {
            HueModule module = new HueModule();
            module.SetCurve(curve);
            return module;
        }

        public static HueModule RandomLerp(float min, float max)
        {
            HueModule module = new HueModule();
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
        internal static extern Submodule* nativeModule_HueModule_Ctor();
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_HueModule_SetNone(Submodule* modulePtr);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_HueModule_SetLerp(Submodule* modulePtr, float end);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_HueModule_SetRandomLerp(Submodule* modulePtr, float min, float max);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_HueModule_SetCurve(Submodule* modulePtr, NativeCurve* curvePtr);
    }
}
