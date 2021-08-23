using System;
using System.Runtime.InteropServices;
using System.Security;
using SE.Core;
using SE.Utility;
using Random = SE.Utility.Random;
using static SE.Particles.ParticleMath;

namespace SE.Particles.Modules
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe class SpriteRotationModule : NativeParticleModule
    {
        private float[] rand;

        private TransitionType transitionType;
        private float start, end;
        private Curve curve;

        private bool IsRandom => transitionType == TransitionType.RandomConstant ||
                                 transitionType == TransitionType.RandomCurve;

        public SpriteRotationModule()
        {
            SubmodulePtr = nativeModule_SpriteRotationModule_Ctor();
        }

        public void SetConstant(float val)
        {
            start = val;
            transitionType = TransitionType.Constant;

            nativeModule_SpriteRotationModule_SetConstant(SubmodulePtr, val);
        }

        public void SetLerp(float start, float end)
        {
            this.start = start;
            this.end = end;
            transitionType = TransitionType.Lerp;

            nativeModule_SpriteRotationModule_SetLerp(SubmodulePtr, start, end);
        }

        public void SetCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = TransitionType.Curve;

            nativeModule_SpriteRotationModule_SetCurve(SubmodulePtr, NativeUtil.CopyCurveToNativeCurve(curve));
        }

        public void SetRandomConstant(float min, float max)
        {
            if (min > max)
                Swap(ref min, ref max);

            start = min;
            end = max;
            transitionType = TransitionType.RandomConstant;
            RegenerateRandom();

            nativeModule_SpriteRotationModule_SetRandomConstant(SubmodulePtr, min, max);
        }

        public void SetRandomCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = TransitionType.RandomCurve;
            RegenerateRandom();
            
            nativeModule_SpriteRotationModule_SetRandomCurve(SubmodulePtr, NativeUtil.CopyCurveToNativeCurve(curve));
        }

        public override void OnInitialize()
        {
            RegenerateRandom();
        }

        private void RegenerateRandom()
        {
            if (!IsRandom || Emitter == null) 
                return;

            rand = new float[Emitter.ParticlesLength];
        }

        public override void OnParticlesActivated(Span<int> particlesIndex)
        {
            if (ParticleEngine.NativeEnabled) {
                return;
            }

            if (!IsRandom)
                return;

            fixed (Particle* particleArr = Emitter.Particles) {
                for (int i = 0; i < particlesIndex.Length; i++) {
                    Particle* particle = &particleArr[particlesIndex[i]];
                    rand[particle->ID] = Random.Next(0.0f, 1.0f);
                }
            }
        }

        public override void OnUpdate(float deltaTime, Particle* arrayPtr, int length)
        {
            if(ParticleEngine.NativeEnabled) {
                return;
            }

            Particle* tail = arrayPtr + length;

            switch (transitionType) {
                case TransitionType.Constant: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->SpriteRotation += start * deltaTime;
                    }
                } break;
                case TransitionType.Lerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float angleDelta = ParticleMath.Lerp(
                            start,
                            end,
                            particle->TimeAlive / particle->InitialLife);

                        particle->SpriteRotation += angleDelta * deltaTime;
                    }
                } break;
                case TransitionType.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->SpriteRotation += curve.Evaluate(particle->TimeAlive / particle->InitialLife) * deltaTime;
                    }
                } break;
                case TransitionType.RandomConstant: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->SpriteRotation += Between(start, end, rand[particle->ID]) * deltaTime;
                    }
                } break;
                case TransitionType.RandomCurve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->SpriteRotation += curve.Evaluate(rand[particle->ID]) * deltaTime;
                    }
                } break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ParticleModule DeepCopy()
        {
            return new SpriteRotationModule {
                transitionType = transitionType,
                start = start,
                end = end,
                curve = curve.Clone()
            };
        }

        public static SpriteRotationModule Constant(float val)
        {
            SpriteRotationModule module = new SpriteRotationModule();
            module.SetConstant(val);
            return module;
        }

        public static SpriteRotationModule Lerp(float start, float end)
        {
            SpriteRotationModule module = new SpriteRotationModule();
            module.SetLerp(start, end);
            return module;
        }

        public static SpriteRotationModule Curve(Curve curve)
        {
            SpriteRotationModule module = new SpriteRotationModule();
            module.SetCurve(curve);
            return module;
        }

        public static SpriteRotationModule RandomConstant(float min, float max)
        {
            SpriteRotationModule module = new SpriteRotationModule();
            module.SetRandomConstant(min, max);
            return module;
        }

        public static SpriteRotationModule RandomCurve(Curve curve)
        {
            SpriteRotationModule module = new SpriteRotationModule();
            module.SetRandomCurve(curve);
            return module;
        }

        private enum TransitionType
        {
            Constant,
            Lerp,
            Curve,
            RandomConstant,
            RandomCurve
        }

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern Submodule* nativeModule_SpriteRotationModule_Ctor();
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpriteRotationModule_SetNone(Submodule* modulePtr);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpriteRotationModule_SetConstant(Submodule* modulePtr, float val);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpriteRotationModule_SetLerp(Submodule* modulePtr, float start, float end);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpriteRotationModule_SetCurve(Submodule* modulePtr, NativeCurve* curvePtr);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpriteRotationModule_SetRandomConstant(Submodule* modulePtr, float min, float max);
        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpriteRotationModule_SetRandomCurve(Submodule* modulePtr, NativeCurve* curvePtr);
    }
}
