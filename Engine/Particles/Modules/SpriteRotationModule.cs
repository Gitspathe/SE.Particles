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
    public unsafe class SpriteRotationModule : ParticleModule
    {
        private float[] rand;

        private TransitionType transitionType;
        private float start, end;
        private Curve curve;

        private bool IsRandom => transitionType == TransitionType.RandomConstant ||
                                 transitionType == TransitionType.RandomCurve;

        public SpriteRotationModule()
        {
        }

        public void SetConstant(float val)
        {
            start = val;
            transitionType = TransitionType.Constant;
        }

        public void SetLerp(float start, float end)
        {
            this.start = start;
            this.end = end;
            transitionType = TransitionType.Lerp;
        }

        public void SetCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = TransitionType.Curve;
        }

        public void SetRandomConstant(float min, float max)
        {
            if (min > max)
                Swap(ref min, ref max);

            start = min;
            end = max;
            transitionType = TransitionType.RandomConstant;
            RegenerateRandom();
        }

        public void SetRandomCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = TransitionType.RandomCurve;
            RegenerateRandom();
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
            Particle* tail = arrayPtr + length;

            switch (transitionType) {
                case TransitionType.Constant: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->SpriteRotation += start * deltaTime;
                    }
                }
                break;
                case TransitionType.Lerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float angleDelta = ParticleMath.Lerp(
                            start,
                            end,
                            particle->TimeAlive / particle->InitialLife);

                        particle->SpriteRotation += angleDelta * deltaTime;
                    }
                }
                break;
                case TransitionType.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->SpriteRotation += curve.Evaluate(particle->TimeAlive / particle->InitialLife) * deltaTime;
                    }
                }
                break;
                case TransitionType.RandomConstant: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->SpriteRotation += Between(start, end, rand[particle->ID]) * deltaTime;
                    }
                }
                break;
                case TransitionType.RandomCurve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->SpriteRotation += curve.Evaluate(rand[particle->ID]) * deltaTime;
                    }
                }
                break;
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
    }
}
