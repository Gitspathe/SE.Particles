using System;
using System.Runtime.InteropServices;
using System.Security;
using SE.Core;
using SE.Utility;
using Random = SE.Utility.Random;

namespace SE.Particles.Modules
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe class SpeedModule : NativeParticleModule
    {
        public bool AbsoluteValue = false;
        
        private float[] rand;

        private Transition transitionType;
        private float start, end;
        private Curve curve;

        private bool IsRandom => transitionType == Transition.RandomCurve;

        public SpeedModule()
        {
            SubmodulePtr = nativeModule_SpeedModule_Ctor();
        }

        public void SetLerp(float start, float end)
        {
            this.start = start;
            this.end = end;
            transitionType = Transition.Lerp;

            nativeModule_SpeedModule_SetLerp(SubmodulePtr, start, end);
        }

        public void SetCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = Transition.Curve;

            nativeModule_SpeedModule_SetCurve(SubmodulePtr, NativeUtil.CopyCurveToNativeCurve(curve));
        }

        public void SetRandomCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = Transition.RandomCurve;
            RegenerateRandom();

            nativeModule_SpeedModule_SetRandomCurve(SubmodulePtr, NativeUtil.CopyCurveToNativeCurve(curve));
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
            if (ParticleEngine.NativeEnabled) {
                return;
            }

            Particle* tail = arrayPtr + length;

            switch (transitionType) {
                case Transition.Lerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float velocity = ParticleMath.Lerp(start, end, particle->TimeAlive / particle->InitialLife);
                        particle->Speed = AbsoluteValue
                            ? velocity
                            : particle->Speed + (velocity * deltaTime);
                    }
                } break;
                case Transition.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float velocity = curve.Evaluate(particle->TimeAlive / particle->InitialLife);
                        particle->Speed = AbsoluteValue
                            ? velocity
                            : particle->Speed + (velocity * deltaTime);
                    }
                } break;
                case Transition.RandomCurve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float velocity = curve.Evaluate(rand[particle->ID]);
                        particle->Speed = AbsoluteValue 
                            ? velocity
                            : particle->Speed + (velocity * deltaTime);
                    }
                } break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ParticleModule DeepCopy()
            => new SpeedModule {
                transitionType = transitionType,
                start = start,
                end = end,
                curve = curve.Clone()
            };

        public static SpeedModule Lerp(float start, float end)
        {
            SpeedModule module = new SpeedModule();
            module.SetLerp(start, end);
            return module;
        }

        public static SpeedModule Curve(Curve curve)
        {
            SpeedModule module = new SpeedModule();
            module.SetCurve(curve);
            return module;
        }

        public static SpeedModule RandomCurve(Curve curve)
        {
            SpeedModule module = new SpeedModule();
            module.SetRandomCurve(curve);
            return module;
        }

        private enum Transition
        {
            Lerp,
            Curve,
            RandomCurve
        }

        protected override void OnModuleModeChanged()
        {
            //throw new NotImplementedException();
        }

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern Submodule* nativeModule_SpeedModule_Ctor();

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool nativeModule_SpeedModule_GetAbsoluteValue(Submodule* modulePtr);

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpeedModule_SetAbsoluteValue(Submodule* modulePtr, bool val);

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpeedModule_SetNone(Submodule* modulePtr);

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpeedModule_SetLerp(Submodule* modulePtr, float start, float end);

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpeedModule_SetCurve(Submodule* modulePtr, NativeCurve* curvePtr);

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void nativeModule_SpeedModule_SetRandomCurve(Submodule* modulePtr, NativeCurve* curvePtr);
    }
}
