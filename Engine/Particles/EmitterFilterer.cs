using SE.Core.Extensions;
using SE.Utility;
using System;

namespace SE.Particles
{
    /// <summary>
    /// Builder-type object which filters a collection of emitters based on various criteria.
    /// </summary>
    public ref struct EmitterFilterer
    {
        private QuickList<Emitter> emitters;

        public EmitterFilterer(QuickList<Emitter> emitters)
        {
            this.emitters = emitters;
        }

        public static implicit operator EmitterFilterer(QuickList<Emitter> emitters)
            => new EmitterFilterer(emitters);

        public static implicit operator QuickList<Emitter>(EmitterFilterer filter)
            => filter.emitters;

        public EmitterFilterer Include(Func<Emitter, bool> predicate)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (!predicate.Invoke(e)) {
                    emitters.Remove(e);
                }
            }
            return this;
        }

        public EmitterFilterer Exclude(Func<Emitter, bool> predicate)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (predicate.Invoke(e)) {
                    emitters.Remove(e);
                }
            }
            return this;
        }

        public EmitterFilterer IncludeLayer(int layer)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (e.Layer != layer) {
                    emitters.Remove(e);
                }
            }
            return this;
        }

        public EmitterFilterer ExcludeLayer(int layer)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (e.Layer == layer) {
                    emitters.Remove(e);
                }
            }
            return this;
        }

        public EmitterFilterer IncludeLayers(int[] layers)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (layers.Contains(e.Layer)) {
                    emitters.Remove(e);
                }
            }
            return this;
        }

        public EmitterFilterer ExcludeLayers(int[] layers)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (layers.Contains(e.Layer)) {
                    emitters.Remove(e);
                }
            }
            return this;
        }

        public EmitterFilterer IncludeBlendMode(BlendMode blendMode)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (e.BlendMode != blendMode) {
                    emitters.Remove(e);
                }
            }
            return this;
        }

        public EmitterFilterer ExcludeBlendMode(BlendMode blendMode)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (e.BlendMode == blendMode) {
                    emitters.Remove(e);
                }
            }
            return this;
        }

        public EmitterFilterer IncludeSpace(Space space)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (e.Space != space) {
                    emitters.Remove(e);
                }
            }
            return this;
        }

        public EmitterFilterer ExcludeSpace(Space space)
        {
            Emitter[] arr = emitters.Array;
            for (int i = emitters.Count - 1; i >= 0; i--) {
                Emitter e = arr[i];
                if (e.Space == space) {
                    emitters.Remove(e);
                }
            }
            return this;
        }
    }
}
