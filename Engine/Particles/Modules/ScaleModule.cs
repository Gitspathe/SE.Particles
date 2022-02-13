using SE.Core;
using System;
using System.Runtime.InteropServices;
using System.Security;
using static SE.Particles.ParticleMath;
using Curve = SE.Utility.Curve;
using Random = SE.Utility.Random;
using Vector2 = System.Numerics.Vector2;

namespace SE.Particles.Modules
{
    [SuppressUnmanagedCodeSecurity]
    public unsafe class ScaleModule : ParticleModule
    {
        public bool AbsoluteValue {
            get => absoluteValue;
            set {
                if (value == absoluteValue)
                    return;

                absoluteValue = value;
            }
        }
        private bool absoluteValue = false;

        private Vector2[] startScales;
        private float[] rand;

        private Transition transitionType;
        private float start, end;
        private Curve curve;

        private bool IsRandom => transitionType == Transition.RandomCurve;

        public ScaleModule()
        {
        }

        public void SetLerp(float start, float end)
        {
            this.start = start;
            this.end = end;
            transitionType = Transition.Lerp;
        }

        public void SetCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = Transition.Curve;
        }

        public void SetRandomCurve(Curve curve)
        {
            this.curve = curve;
            transitionType = Transition.RandomCurve;
            RegenerateRandom();
        }

        public override void OnInitialize()
        {
            RegenerateRandom();
            startScales = new Vector2[Emitter.ParticlesLength];
        }

        private void RegenerateRandom()
        {
            if (!IsRandom || Emitter == null)
                return;

            rand = new float[Emitter.ParticlesLength];
        }

        public override void OnParticlesActivated(Span<int> particlesIndex)
        {
            fixed (Particle* particleArr = Emitter.Particles) {
                for (int i = 0; i < particlesIndex.Length; i++) {
                    Particle* particle = &particleArr[particlesIndex[i]];
                    startScales[particle->ID] = particle->Scale;
                    if (!IsRandom)
                        continue;

                    rand[particle->ID] = Random.Next(0.0f, 1.0f);
                }
            }
        }

        public override void OnUpdate(float deltaTime, Particle* arrayPtr, int length)
        {
            Particle* tail = arrayPtr + length;

            switch (transitionType) {
                case Transition.Lerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float scale = Between(start, end, particle->TimeAlive / particle->InitialLife);
                        particle->Scale = AbsoluteValue
                            ? new Vector2(scale, scale)
                            : new Vector2(scale, scale) * startScales[particle->ID];
                    }
                }
                break;
                case Transition.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float scale = curve.Evaluate(particle->TimeAlive / particle->InitialLife);
                        particle->Scale = AbsoluteValue
                            ? new Vector2(scale, scale)
                            : new Vector2(scale, scale) * startScales[particle->ID];
                    }
                }
                break;
                case Transition.RandomCurve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float scale = curve.Evaluate(rand[particle->ID]);
                        particle->Scale = AbsoluteValue
                            ? new Vector2(scale, scale)
                            : new Vector2(scale, scale) * startScales[particle->ID];
                    }
                }
                break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override ParticleModule DeepCopy()
            => new ScaleModule {
                transitionType = transitionType,
                start = start,
                end = end,
                curve = curve.Clone()
            };

        public static ScaleModule Lerp(float start, float end)
        {
            ScaleModule module = new ScaleModule();
            module.SetLerp(start, end);
            return module;
        }

        public static ScaleModule Curve(Curve curve)
        {
            ScaleModule module = new ScaleModule();
            module.SetCurve(curve);
            return module;
        }

        public static ScaleModule RandomCurve(Curve curve)
        {
            ScaleModule module = new ScaleModule();
            module.SetRandomCurve(curve);
            return module;
        }

        private enum Transition
        {
            Lerp,
            Curve,
            RandomCurve
        }
    }
}
