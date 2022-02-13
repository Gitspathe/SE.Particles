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
    public unsafe class LightnessModule : ParticleModule
    {
        private float[] startLits;
        private float[] randEndLits;

        private Transition transitionType;
        private float end1;
        private float end2;
        private Curve curve;

        private bool IsRandom => transitionType == Transition.RandomLerp;

        public LightnessModule()
        {
        }

        public void SetLerp(float end)
        {
            end1 = end;
            transitionType = Transition.Lerp;
        }

        public void SetCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = Transition.Curve;
        }

        public void SetRandomLerp(float min, float max)
        {
            if (min > max)
                Swap(ref min, ref max);

            end1 = min;
            end2 = max;
            transitionType = Transition.RandomLerp;
        }

        public override ParticleModule DeepCopy()
            => new LightnessModule {
                transitionType = transitionType,
                end1 = end1,
                end2 = end2,
                curve = curve.Clone()
            };

        public override void OnInitialize()
        {
            startLits = new float[Emitter.ParticlesLength];
            RegenerateRandom();
        }

        private void RegenerateRandom()
        {
            if (!IsRandom || Emitter == null)
                return;

            randEndLits = new float[Emitter.ParticlesLength];
        }

        public override void OnParticlesActivated(Span<int> particlesIndex)
        {
            fixed (Particle* particleArr = Emitter.Particles) {
                for (int i = 0; i < particlesIndex.Length; i++) {
                    Particle* particle = &particleArr[particlesIndex[i]];
                    startLits[particle->ID] = particle->Color.Lightness;
                    if (!IsRandom)
                        continue;

                    randEndLits[particle->ID] = Between(end1, end2, Random.Next(0.0f, 1.0f));
                }
            }
        }

        public override void OnUpdate(float deltaTime, Particle* arrayPtr, int length)
        {
            Particle* tail = arrayPtr + length;
            int i = 0;

            switch (transitionType) {
                case Transition.Lerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++, i++) {
                        particle->Color.Lightness = ParticleMath.Lerp(startLits[particle->ID], end1, particle->TimeAlive / particle->InitialLife);
                    }
                }
                break;
                case Transition.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float lifeRatio = particle->TimeAlive / particle->InitialLife;
                        particle->Color.Lightness = (byte)curve.Evaluate(lifeRatio);
                    }
                }
                break;
                case Transition.RandomLerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++, i++) {
                        particle->Color.Lightness = ParticleMath.Lerp(startLits[particle->ID], randEndLits[particle->ID], particle->TimeAlive / particle->InitialLife);
                    }
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static LightnessModule Lerp(float end)
        {
            LightnessModule module = new LightnessModule();
            module.SetLerp(end);
            return module;
        }

        public static LightnessModule Curve(Curve curve)
        {
            LightnessModule module = new LightnessModule();
            module.SetCurve(curve);
            return module;
        }

        public static LightnessModule RandomLerp(float min, float max)
        {
            LightnessModule module = new LightnessModule();
            module.SetRandomLerp(min, max);
            return module;
        }

        private enum Transition
        {
            Lerp,
            Curve,
            RandomLerp
        }
    }
}
