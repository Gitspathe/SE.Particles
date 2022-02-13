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
    public unsafe class AlphaModule : ParticleModule
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
            RegenerateRandom();
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
            fixed (Particle* particleArr = Emitter.Particles) {
                for (int i = 0; i < particlesIndex.Length; i++) {
                    Particle* particle = &particleArr[particlesIndex[i]];
                    startAlphas[particle->ID] = particle->Color.Alpha;
                    if (!IsRandom)
                        continue;

                    randEndAlphas[particle->ID] = Between(end1, end2, Random.Next(0.0f, 1.0f));
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
                        particle->Color.Alpha = ParticleMath.Lerp(startAlphas[particle->ID], end1, particle->TimeAlive / particle->InitialLife);
                    }
                }
                break;
                case Transition.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float lifeRatio = particle->TimeAlive / particle->InitialLife;
                        particle->Color.Alpha = curve.Evaluate(lifeRatio);
                    }
                }
                break;
                case Transition.RandomLerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++, i++) {
                        particle->Color.Alpha = ParticleMath.Lerp(startAlphas[particle->ID], randEndAlphas[particle->ID], particle->TimeAlive / particle->InitialLife);
                    }
                }
                break;
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
    }
}
