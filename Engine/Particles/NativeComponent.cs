using SE.Particles.Modules;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace SE.Particles
{
    [SuppressUnmanagedCodeSecurity]
    public sealed unsafe class NativeComponent
    {
        private Module* nativePtr;
        private Emitter emitter;

        public NativeComponent(Emitter emitter)
        {
            nativePtr = nativeModule_Create();
            this.emitter = emitter;
        }

        internal void OnUpdate(float deltaTime, Particle* particleArrPtr, int length)
        {
            nativeModule_OnUpdate(nativePtr, deltaTime, particleArrPtr, length);
        }

        internal void OnParticlesActivated(Span<int> particlesIndex)
        {
            fixed (int* particleIndexArr = particlesIndex) {
                nativeModule_OnParticlesActivated(nativePtr, particleIndexArr, emitter.GetParticlePointer(), particlesIndex.Length);
            }
        }

        internal void InitializeSubmodule(NativeParticleModule module, int particleArrayLength)
        {
            nativeModule_OnInitialize(nativePtr, module.SubmodulePtr, particleArrayLength);
        }

        internal void AddSubmodule(NativeParticleModule module)
        {
            nativeModule_addSubmodule(nativePtr, module.SubmodulePtr);
        }

        internal void RemoveSubmodule(NativeParticleModule module)
        {
            nativeModule_removeSubmodule(nativePtr, module.SubmodulePtr);
        }

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        private static extern void nativeModule_addSubmodule(Module* modulePtr, Submodule* submodulePtr);

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        private static extern void nativeModule_removeSubmodule(Module* modulePtr, Submodule* submodulePtr);

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        private static extern Module* nativeModule_Create();

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        private static extern void nativeModule_OnUpdate(Module* modulePtr, float deltaTime, Particle* particleArrPtr, int length);

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        private static extern void nativeModule_OnParticlesActivated(Module* modulePtr, int* particleIndexArr, Particle* particleArrPtr, int length);

        [DllImport("SE.Native", CallingConvention = CallingConvention.Cdecl)]
        private static extern void nativeModule_OnInitialize(Module* modulePtr, Submodule* submodulePtr, int particleArrayLength);
    }
}
