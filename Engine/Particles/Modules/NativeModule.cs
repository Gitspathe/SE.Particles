using System;
using System.Runtime.InteropServices;

namespace SE.Particles.Modules
{
    // TODO.
    // TODO: Native module builder.
    public unsafe class NativeModule : ParticleModule, IDisposable
    {
        public NativeAlphaModuleWrapper AlphaModule;

        private void* nativeModulePtr;

        private bool isDisposed;

        public NativeModule()
        {
            nativeModulePtr = nativeModule_Create();
            AlphaModule = new NativeAlphaModuleWrapper(nativeModulePtr);
        }

        public override void OnParticlesActivated(Span<int> particlesIndex)
        {
            fixed (int* indexPtr = particlesIndex) {
                fixed (Particle* particleArrPtr = Emitter.Particles) {
                    nativeModule_OnParticlesActivated(nativeModulePtr, indexPtr, particleArrPtr, particlesIndex.Length);
                }
            }
        }

        public override void OnUpdate(float deltaTime, Particle* arrayPtr, int length)
        {
            nativeModule_Update(nativeModulePtr, deltaTime, arrayPtr, length);
        }

        public override void OnInitialize()
        {
            nativeModule_Initialize(nativeModulePtr, Emitter.Particles.Length);
        }

        public override ParticleModule DeepCopy()
        {
            return null;
        }

        ~NativeModule()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            nativeModule_Delete(nativeModulePtr);
            isDisposed = true;
        }

        public struct NativeAlphaModuleWrapper
        {
            private void* modulePtr;

            internal NativeAlphaModuleWrapper(void* modulePtr) => this.modulePtr = modulePtr;
            public void SetNone() => nativeModule_AlphaModule_SetNone(modulePtr);
            public void SetLerp(float end) => nativeModule_AlphaModule_SetLerp(modulePtr, end);
            public void SetRandomLerp(float min, float max) => nativeModule_AlphaModule_SetRandomLerp(modulePtr, min, max);
        }

        [DllImport("SE.Native")]
        private static extern void* nativeModule_Create();
        [DllImport("SE.Native")]
        private static extern void* nativeModule_Initialize(void* modulePtr, int particleArrayLength);
        [DllImport("SE.Native")]
        private static extern void* nativeModule_OnParticlesActivated(void* modulePtr, int* particleIndexArr, Particle* particleArrPtr, int length);
        [DllImport("SE.Native")]
        private static extern void* nativeModule_Update(void* modulePtr, float deltaTime, Particle* particleArrPtr, int length);
        [DllImport("SE.Native")]
        private static extern void* nativeModule_Delete(void* modulePtr);

        [DllImport("SE.Native")]
        private static extern void* nativeModule_AlphaModule_SetNone(void* modulePtr);
        [DllImport("SE.Native")]
        private static extern void* nativeModule_AlphaModule_SetLerp(void* modulePtr, float end);
        [DllImport("SE.Native")]
        private static extern void* nativeModule_AlphaModule_SetRandomLerp(void* modulePtr, float min, float max);
    }
}
