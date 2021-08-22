using System.Numerics;
using SE.Engine.Utility;
using SE.Utility;
using System;
using SE.Core;
using SE.Core.Exceptions;
#if MONOGAME
using Microsoft.Xna.Framework.Graphics;
#endif

namespace SE.Particles
{
    public class EmitterConfig
    {
        public ColorConfig Color;
        public ScaleConfig Scale;
        public LifeConfig Life;
        public SpeedConfig Speed;
        public EmissionConfig Emission;

    #if MONOGAME
        public TextureConfig Texture;
    #endif

        protected Emitter Parent;

        internal EmitterConfig(Emitter parent)
        {
            Parent = parent;
            Color = new ColorConfig(parent);
            Scale = new ScaleConfig(parent);
            Life = new LifeConfig(parent);
            Speed = new SpeedConfig(parent);
            Emission = new EmissionConfig(parent);

        #if MONOGAME
            Texture = new TextureConfig(parent);
        #endif
        }

        internal void SetParent(Emitter newParent)
        {
            Parent = newParent;
            Color.SetParent(newParent);
            Scale.SetParent(newParent);
            Life.SetParent(newParent);
            Speed.SetParent(newParent);
            Emission.SetParent(newParent);

        #if MONOGAME
            Texture.SetParent(newParent);
        #endif
        }

        public abstract class EmitterConfigCollection
        {
            protected Emitter Parent;

            internal void SetParent(Emitter parent)
            {
                Parent = parent;
            }

            internal EmitterConfigCollection(Emitter parent)
            {
                Parent = parent;
            }
        }

        public class ColorConfig : EmitterConfigCollection
        {
            internal StartingValue StartValueType;
            internal Vector4 Min, Max;
            internal Curve4 Curve;

            internal ColorConfig(Emitter parent) : base(parent)
            {
                Min = new Vector4(0, 1.0f, 1.0f, 1.0f);
                StartValueType = StartingValue.Normal;
            }

            public void SetNormal(Vector4 value)
            {
                Min = value;
                StartValueType = StartingValue.Normal;
            }

            public void SetRandomBetween(Vector4 min, Vector4 max)
            {
                Min = min;
                Max = max;
                StartValueType = StartingValue.Random;
            }

            public void SetRandomCurve(Curve4 curve)
            {
                Curve = curve;
                StartValueType = StartingValue.RandomCurve;
            }

            public ColorConfig DeepCopy()
                => new ColorConfig(Parent) {
                    StartValueType = StartValueType,
                    Min = Min,
                    Max = Max,
                    Curve = Curve
                };
        }

        public class ScaleConfig : EmitterConfigCollection
        {
            internal StartingValue StartValueType;
            internal Vector2 Min, Max;
            internal Curve2 Curve;
            internal bool TwoDimensions;

            internal ScaleConfig(Emitter parent) : base(parent)
            {
                StartValueType = StartingValue.Normal;
                Min = new Vector2(1.0f, 1.0f);
            }

            public void SetNormal(Vector2 value)
            {
                Min = value;
                TwoDimensions = true;
                StartValueType = StartingValue.Normal;
            }

            public void SetNormal(float value)
            {
                Min = new Vector2(value, value);
                TwoDimensions = false;
                StartValueType = StartingValue.Normal;
            }

            public void SetRandomBetween(Vector2 min, Vector2 max)
            {
                Min = min;
                Max = max;
                TwoDimensions = true;
                StartValueType = StartingValue.Random;
            }

            public void SetRandomBetween(float min, float max)
            {
                Min = new Vector2(min, min);
                Max = new Vector2(max, max);
                TwoDimensions = false;
                StartValueType = StartingValue.Random;
            }

            public void SetRandomCurve(Curve2 curve)
            {
                Curve = curve;
                TwoDimensions = true;
                StartValueType = StartingValue.RandomCurve;
            }

            public void SetRandomCurve(Curve curve)
            {
                Curve = new Curve2(curve, curve);
                TwoDimensions = false;
                StartValueType = StartingValue.RandomCurve;
            }

            public ScaleConfig DeepCopy() 
                => new ScaleConfig(Parent) {
                    StartValueType = StartValueType,
                    Min = Min,
                    Max = Max,
                    Curve = Curve
                };
        }

        public class LifeConfig : EmitterConfigCollection
        {
            internal StartingValue StartValueType;
            internal float Min, Max;
            internal Curve Curve;

            internal float maxLife;

            internal LifeConfig(Emitter parent) : base(parent) { }

            public void SetNormal(float value)
            {
                Min = value;
                StartValueType = StartingValue.Normal;
                maxLife = Min;
            }

            public void SetRandomBetween(float min, float max)
            {
                Min = min;
                Max = max;
                StartValueType = StartingValue.Random;
                maxLife = max;
            }

            public void SetRandomCurve(Curve curve)
            {
                Curve = curve;
                StartValueType = StartingValue.RandomCurve;

                float maxFound = 0.0f;
                foreach (CurveKey curveKey in curve.Keys) {
                    if (curveKey.Value > maxFound) {
                        maxFound = curveKey.Value;
                    }
                }
                maxLife = maxFound;
            }

            public LifeConfig DeepCopy() 
                => new LifeConfig(Parent) {
                    StartValueType = StartValueType,
                    Min = Min,
                    Max = Max,
                    Curve = Curve
                };
        }

        public class SpeedConfig : EmitterConfigCollection
        {
            internal StartingValue StartValueType;
            internal float Min, Max;
            internal Curve Curve;

            internal SpeedConfig(Emitter parent) : base(parent) { }

            public void SetNormal(float value)
            {
                Min = value;
                StartValueType = StartingValue.Normal;
            }

            public void SetRandomBetween(float min, float max)
            {
                Min = min;
                Max = max;
                StartValueType = StartingValue.Random;
            }

            public void SetRandomCurve(Curve curve)
            {
                Curve = curve;
                StartValueType = StartingValue.RandomCurve;
            }

            public SpeedConfig DeepCopy() 
                => new SpeedConfig(Parent) {
                    StartValueType = StartValueType,
                    Min = Min,
                    Max = Max,
                    Curve = Curve
                };
        }

    #if MONOGAME
        public class TextureConfig : EmitterConfigCollection
        {
            internal TextureStartingValue StartingValue;
            internal Texture2D Texture;
            internal Vector2 Size;
            internal Vector2 FullTextureSize;

            internal TextureConfig(Emitter parent) : base(parent) { }

            public void SetWhole(Texture2D texture)
            {
                StartingValue = TextureStartingValue.Whole;
                Texture = texture;
                Size = new Vector2(texture.Width, texture.Height);
                FullTextureSize = new Vector2(texture.Width, texture.Height);
                Parent.Renderer?.OnParticleSizeChanged();
            }

            public void SetSlice(Texture2D texture, Vector2 size)
            {
                StartingValue = TextureStartingValue.Slice;
                Texture = texture;

                try {
                    if (size.X <= 0 || size.Y <= 0)
                        throw new InvalidEmitterValueException($"{nameof(size)} must have values greater than zero.");

                    Size = size;
                } catch (Exception) {
                    if (ParticleEngine.ErrorHandling == ErrorHandling.Throw)
                        throw;

                    Size = new Vector2(texture.Width, texture.Height);
                }

                FullTextureSize = new Vector2(texture.Width, texture.Height);
                Parent.Renderer?.OnParticleSizeChanged();
            }

            public void SetSheet(Texture2D texture, int columns, int rows)
            {
                Texture = texture;
                FullTextureSize = new Vector2(texture.Width, texture.Height);
                Size = new Vector2(FullTextureSize.X / columns, FullTextureSize.Y / rows);
                Parent.Renderer?.OnParticleSizeChanged();
            }

            public TextureConfig DeepCopy() => 
                new TextureConfig(Parent) {
                    StartingValue = StartingValue,
                    Texture = Texture,
                    Size = Size,
                    FullTextureSize = FullTextureSize
                };
        }
#endif

        public class EmissionConfig : EmitterConfigCollection
        {
            public bool IsPlaying { get; internal set; }
            public float CurrentTime { get; internal set; }
            public bool Loop { get; set; }

            public float Duration {
                get => duration;
                set {
                    if (value < 0.0f)
                        value = 0.0f;

                    duration = value;
                }
            }
            private float duration = 1.0f;

            internal EmissionType EmissionType;
            internal float QueuedParticles;
            internal float ConstantValue;
            internal Curve CurveValue;

            internal EmissionConfig(Emitter parent) : base(parent) { }

            public void SetConstant(float value)
            {
                if (value < 0) {
                    EmissionType = EmissionType.None;
                    return;
                }

                EmissionType = EmissionType.Constant;
                ConstantValue = value;
            }

            public void SetCurve(Curve curve)
            {
                if(curve == null || curve.Keys.Count == 0) {
                    EmissionType = EmissionType.None;
                    return;
                }

                EmissionType = EmissionType.Curve;
                CurveValue = curve;
            }

            public EmissionConfig DeepCopy() =>
                new EmissionConfig(Parent) {
                    Loop = Loop,
                    Duration = Duration,
                    EmissionType = EmissionType,
                    QueuedParticles = 0,
                    ConstantValue = ConstantValue,
                    CurveValue = CurveValue.Clone()
                };
        }

        internal enum EmissionType
        {
            None,
            Constant,
            Curve
        }

        internal enum StartingValue
        {
            Normal,
            Random,
            RandomCurve
        }

        internal enum TextureStartingValue
        {
            Whole,
            Slice,
            Sheet
        }

        public EmitterConfig DeepCopy() 
            => new EmitterConfig(Parent) {
                Color = Color.DeepCopy(),
                Life = Life.DeepCopy(),
                Scale = Scale.DeepCopy(),
                Speed = Speed.DeepCopy(),
                Texture = Texture.DeepCopy(),
                Emission = Emission.DeepCopy()
            };
    }

}
