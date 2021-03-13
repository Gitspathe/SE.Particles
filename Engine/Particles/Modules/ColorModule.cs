using System;
using System.Numerics;
using SE.Engine.Utility;
using SE.Utility;
using Random = SE.Utility.Random;
using static SE.Particles.ParticleMath;

namespace SE.Particles.Modules
{
    public unsafe class ColorModule : ParticleModule
    {
        private Vector4[] startColors;
        private Vector4[] randEndColors;

        private Transition transitionType;
        private Vector4 end1;
        private Vector4 end2;
        private Curve curveH, curveS, curveL, curveA;

        private bool IsRandom => transitionType == Transition.RandomLerp;

        public void SetLerp(Vector4 end)
        {
            end1 = end;
            transitionType = Transition.Lerp;
        }

        public void SetCurve(Curve h, Curve s, Curve l, Curve a)
        {
            curveH = h;
            curveS = s;
            curveL = l;
            curveA = a;
            transitionType = Transition.Curve;
        }

        public void SetRandomLerp(Vector4 min, Vector4 max)
        {
            if (min.X > max.X)
                Swap(ref min.X, ref max.X);
            if (min.Y > max.Y)
                Swap(ref min.Y, ref max.Y);
            if (min.Z > max.Z)
                Swap(ref min.Z, ref max.Z);
            if (min.W > max.W)
                Swap(ref min.W, ref max.W);

            end1 = min;
            end2 = max;
            transitionType = Transition.RandomLerp;
        }

        public override ParticleModule DeepCopy() 
            => new ColorModule {
                transitionType = transitionType,
                end1 = end1,
                end2 = end2,
                curveH = curveH.Clone(),
                curveS = curveS.Clone(),
                curveL = curveL.Clone(),
                curveA = curveA.Clone()
            };

        public override void OnInitialize()
        {
            startColors = new Vector4[Emitter.ParticlesLength];
            RegenerateRandom();
        }

        private void RegenerateRandom()
        {
            if (!IsRandom || Emitter == null) 
                return;

            randEndColors = new Vector4[Emitter.ParticlesLength];
        }

        public override void OnParticlesActivated(Span<int> particlesIndex)
        {
            fixed (Particle* particleArr = Emitter.Particles) {
                for (int i = 0; i < particlesIndex.Length; i++) {
                    Particle* particle = &particleArr[particlesIndex[i]];
                    startColors[particle->ID] = particle->Color;
                    if (!IsRandom)
                        continue;

                    randEndColors[particle->ID] = new Vector4(
                        Between(end1.X, end2.X, Random.Next(0.0f, 1.0f)),
                        Between(end1.Y, end2.Y, Random.Next(0.0f, 1.0f)),
                        Between(end1.Z, end2.Z, Random.Next(0.0f, 1.0f)),
                        Between(end1.W, end2.W, Random.Next(0.0f, 1.0f)));
                }
            }
        }

        public override void OnUpdate(float deltaTime, Particle* arrayPtr, int length)
        {
            Particle* tail = arrayPtr + length;

            switch (transitionType) {
                case Transition.Lerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->Color = Vector4.Lerp(startColors[particle->ID], end1, particle->TimeAlive / particle->InitialLife);
                    }
                } break;
                case Transition.Curve: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        float lifeRatio = particle->TimeAlive / particle->InitialLife;
                        particle->Color = new Vector4(
                            curveH.Evaluate(lifeRatio),
                            curveS.Evaluate(lifeRatio),
                            curveL.Evaluate(lifeRatio),
                            curveA.Evaluate(lifeRatio));
                    }
                } break;
                case Transition.RandomLerp: {
                    for (Particle* particle = arrayPtr; particle < tail; particle++) {
                        particle->Color = Vector4.Lerp(startColors[particle->ID], randEndColors[particle->ID], particle->TimeAlive / particle->InitialLife);
                    }
                } break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static ColorModule Lerp(Vector4 end)
        {
            ColorModule module = new ColorModule();
            module.SetLerp(end);
            return module;
        }

        public static ColorModule Curve(Curve h, Curve s, Curve l, Curve a)
        {
            ColorModule module = new ColorModule();
            module.SetCurve(h, s, l, a);
            return module;
        }

        public static ColorModule Curve(Curve4 curve)
        {
            ColorModule module = new ColorModule();
            module.SetCurve(curve.X, curve.Y, curve.Z, curve.W);
            return module;
        }

        public static ColorModule RandomLerp(Vector4 min, Vector4 max)
        {
            ColorModule module = new ColorModule();
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
