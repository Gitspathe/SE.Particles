using SE.Core;
using SE.Utility;
using System;
using System.Runtime.InteropServices;
using System.Security;
using static SE.Particles.ParticleMath;
using Random = SE.Utility.Random;

namespace SE.Particles.Modules
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe class SaturationModule : NativeParticleModule
    {
        private float[] startSats;
        private float[] randEndSats;

        private Transition transitionType;
        private float end1;
        private float end2;
        private Curve curve;

        private bool IsRandom => transitionType == Transition.RandomLerp;

        public SaturationModule()
        {
            SubmodulePtr = nativeModule_SaturationModule_Ctor();
        }

        public void SetLerp(float end)
        {
            end1 = end;
            transitionType = Transition.Lerp;

            nativeModule_SaturationModule_SetLerp(SubmodulePtr, end);
        }

        public void SetCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = Transition.Curve;

            nativeModule_SaturationModule_SetCurve(SubmodulePtr, NativeUtil.CopyCurveToNativeCurve(curve));
        }

        public void SetRandomLerp(float min, float max)
        {
            if (min > max)
                Swap(ref min, ref max);

            end1 = min;
            end2 = max;
            transitionType = Transition.RandomLerp;

            nativeModule_SaturationModule_SetRandomLerp(SubmodulePtr, min, max);
        }

        public override ParticleModule DeepCopy()
            => new SaturationModule {
                transitionType = transitionType,
                end1 = end1,
                end2 = end2,
                curve = curve.Clone()
            };

        public override void OnInitialize()
        {
            startSats = new float[Emitter.ParticlesLength];
            RegenerateRandom();
        }

        private void RegenerateRandom()
        {
            if (!IsRandom || Emitter == null)
                return;

            randEndSats = new float[Emitter.ParticlesLength];
        }

        public override void OnParticlesActivated(Span<int> particlesIndex)
        {
            if (ParticleEngine.NativeEnabled) {
                return;
            }

            fixed (Particle* particleArr = Emitter.Particles) {
                for (int i = 0; i < particlesIndex.Length; i++) {
                    Particle* particle = &particleArr[particlesIndex[i]];
                    startSats[particle->ID] = particle->Color.Saturation;
                    if (!IsRandom)
                        continue;

                    randEndSats[particle->ID] = Between(end1, end2, Random.Next(0.0f, 1.0f));
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
                        particle->Color.Saturation = ParticleMath.Lerp(startSats[particle->ID], end1, particle->TimeAlive / particle->InitialLife);
                    }
                }
                break;
                case Transition.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float lifeRatio = particle->TimeAlive / particle->InitialLife;
                        particle->Color.Saturation = (byte)curve.Evaluate(lifeRatio);
                    }
                }
                break;
                case Transition.RandomLerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++, i++) {
                        particle->Color.Saturation = ParticleMath.Lerp(startSats[particle->ID], randEndSats[particle->ID], particle->TimeAlive / particle->InitialLife);
                    }
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static SaturationModule Lerp(float end)
        {
            SaturationModule module = new SaturationModule();
            module.SetLerp(end);
            return module;
        }

        public static SaturationModule Curve(Curve curve)
        {
            SaturationModule module = new SaturationModule();
            module.SetCurve(curve);
            return module;
        }

        public static SaturationModule RandomLerp(float min, float max)
        {
            SaturationModule module = new SaturationModule();
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
        internal static extern Submodule* nativeModule_SaturationModule_Ctor();
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SaturationModule_SetNone(Submodule* modulePtr);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SaturationModule_SetLerp(Submodule* modulePtr, float end);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SaturationModule_SetRandomLerp(Submodule* modulePtr, float min, float max);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SaturationModule_SetCurve(Submodule* modulePtr, NativeCurve* curvePtr);
    }
}
