using System;
using System.Runtime.InteropServices;

namespace SE.Particles.Modules
{
    // TODO.
    // TODO: Native module builder.
    public unsafe class NativeModule : ParticleModule, IDisposable
    {
        private void* nativeModulePtr;

        private bool isDisposed;

        public NativeModule()
        {
            nativeModulePtr = NativeModule_Create();
        }

        public override void OnUpdate(float deltaTime, Particle* arrayPtr, int length)
        {
            NativeModule_Update(nativeModulePtr, deltaTime, arrayPtr, length);
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

            // TODO: Clean up unmanaged shit.
            isDisposed = true;
        }

        [DllImport("SE.Native")]
        private static extern void* NativeModule_Create();

        [DllImport("SE.Native")]
        private static extern void* NativeModule_Update(void* modulePtr, float deltaTime, Particle* particleArrPtr, int length);
    }
}
