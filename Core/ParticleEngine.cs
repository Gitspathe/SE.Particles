﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SE.Core.Extensions;
using SE.Core.Exceptions;
using SE.Particles;
using SE.Particles.AreaModules;
using SE.Utility;
using System.Reflection;
using System.Threading;
#if MONOGAME
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
#endif

using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

[assembly: InternalsVisibleTo("SE.Particles.Native")]
namespace SE.Core
{
    public static class ParticleEngine
    {
        private static readonly QuickList<Emitter> emitters = new QuickList<Emitter>();
        private static readonly QuickList<Emitter> visibleEmitters = new QuickList<Emitter>();
        private static readonly QuickList<AreaModule> areaModules = new QuickList<AreaModule>();
    
    #if MONOGAME
        internal static GraphicsDevice GraphicsDevice;
        internal static Effect ParticleInstanceEffect;

        // TODO: How will this work now that ParticleRenderer is a thing??
        public static bool UseParticleRenderer {
            get => useParticleRenderer;
            set {
                if(GraphicsDevice == null || useParticleRenderer == value)
                    return;

                useParticleRenderer = value;
            }
        }
        private static bool useParticleRenderer = true;
    #endif

        public static bool NativeEnabled = false;

        internal static bool UseArrayPool => AllocationMode == ParticleAllocationMode.ArrayPool;

        public static int ParticleCount { 
            get  { 
                int total = 0;
                foreach (Emitter emitter in emitters) {
                    if(emitter.Enabled)
                        total += emitter.ActiveParticles.Length;
                }
                return total;
            }
        }

        public static int EmitterCount {
            get {
                int total = 0;
                foreach (Emitter emitter in emitters) {
                    if (emitter.Enabled)
                        total += 1;
                }
                return total;
            }
        }

        /// <summary>Controls how particles are allocated for new emitters. Cannot be changed after Initialize() has been called.</summary>
        public static ParticleAllocationMode AllocationMode {
            get => allocationMode;
            set {
                if (Initialized)
                    throw new InvalidOperationException("Cannot change allocation mode after the particle engine has been initialized.");

                allocationMode = value;
            }
        }
        private static ParticleAllocationMode allocationMode = ParticleAllocationMode.ArrayPool;

        /// <summary>Controls how the particle engine handles minor exceptions. Cannot be changed after Initialize() has been called.</summary>
        public static ErrorHandling ErrorHandling {
            get => errorHandling;
            set {
                if (Initialized)
                    throw new InvalidOperationException("Cannot change error handling mode after the particle engine has been initialized.");

                errorHandling = value;
            }
        }
        private static ErrorHandling errorHandling = ErrorHandling.Stability;

        /// <summary>Maximum time delta the emission rate will be clamped to. Increases stability when the frame-rate is unstable.</summary>
        public static float MaxEmissionTimeStep {
            get => maxEmissionTimeStep;
            set => maxEmissionTimeStep = ParticleMath.Clamp(value, 1.0f / 144.0f, 1.0f);
        }
        private static float maxEmissionTimeStep = 0.1f;

        /// <summary>If true, the particle engine will use the CLR ThreadPool. If false, particle processing threads will be created.
        ///          Generally 'false' will result in better performance. Try setting to true if performance is unstable.</summary>
        public static bool UseThreadPool {
            get => useThreadPool;
            set {
                if(useThreadPool == value)
                    return;

                if (updateTask != null && !updateTask.IsCompleted) {
                    updateTask.Wait();
                }

                useThreadPool = value;
                if (!useThreadPool && threads == null) {
                    CreateThreads();
                } else if (useThreadPool && threads != null) {
                    EndThreads();
                }
            }
        }
        private static bool useThreadPool = false;

        /// <summary>Controls how the particle engine updates.</summary>
        public static UpdateMode UpdateMode = UpdateMode.ParallelAsynchronous;
        
        public static bool Initialized { get; private set; }

        private static Vector4[] tmpViewArr = new Vector4[1];
        private static QuickList<Emitter> pendingDestroy = new QuickList<Emitter>();
        private static Task updateTask;

        private static QuickList<ParticleThread> threads;

    #if MONOGAME
        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            if(graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));

            GraphicsDevice = graphicsDevice;

            // Disgusting reflection to determine shader profile.
            int val;
            try {
                Assembly mgAssembly = Assembly.GetAssembly(typeof(Game));
                Type shaderType = mgAssembly.GetType("Microsoft.Xna.Framework.Graphics.Shader");
                PropertyInfo profileProperty = shaderType.GetProperty("Profile");
                val = (int)profileProperty.GetValue(null);
            } catch (Exception) {
                throw new ParticleEngineInitializationException("Unable to read shader profile. MonoGame assembly could be incompatible or missing.");
            }

            // 0 = OpenGL, 1 = DirectX.
            if(val == 0) {
                ParticleInstanceEffect = new Effect(graphicsDevice, File.ReadAllBytes("CompiledInstancingShaderOpenGL.mgfx"));
            } else if(val == 1) {
                ParticleInstanceEffect = new Effect(graphicsDevice, File.ReadAllBytes("CompiledInstancingShaderDirectX.mgfx"));
            } else {
                throw new ParticleEngineInitializationException($"Unable to initialize particle engine. Unrecognized shader profile '{val}'");
            }
            ParticleInstanceEffect.CurrentTechnique = ParticleInstanceEffect.Techniques["ParticleInstancing"];

            if (!UseThreadPool) {
                CreateThreads();
            }

            Initialized = true;
        }
    #else
        public static void Initialize()
        {
            Initialized = true;
        }
    #endif

        /// <summary>
        /// Starts an update of the particle engine.
        /// </summary>
        /// <param name="deltaTime">Time passed since last update in seconds.</param>
        /// <param name="viewBounds">Rectangle representing the 2D view port (X, Y, Width, Height).</param>
        public static void Update(float deltaTime, Vector4 viewBounds)
        {
            tmpViewArr[0] = viewBounds;
            Update(deltaTime, tmpViewArr);
        }

        /// <summary>
        /// Starts an update of the particle engine. Supports multiple view ports.
        /// </summary>
        /// <param name="deltaTime">Time passed since last update in seconds.</param>
        /// <param name="viewBounds">Rectangles representing the 2D view ports (X, Y, Width, Height).</param>
        public static void Update(float deltaTime, Span<Vector4> viewBounds = default)
        {
            if (!Initialized)
                throw new InvalidOperationException("Particle engine has not been initialized. Call ParticleEngine.Initialize() first.");

            WaitForThreads();
            DestroyPending(deltaTime);
            FindVisible(viewBounds);

            // If visible emitters is zero, there's nothing to do.
            if(visibleEmitters.Count == 0) {
                return;
            }

            switch (UpdateMode) {
                case UpdateMode.ParallelAsynchronous: {
                    CreateTasks(deltaTime);
                } break;
                case UpdateMode.ParallelSynchronous: {
                    CreateTasks(deltaTime);
                    WaitForThreads();
                } break;
                case UpdateMode.Synchronous: {
                    // Update area modules.
                    foreach (AreaModule aMod in areaModules) {
                        for (int i = 0; i < emitters.Count; i++) {
                            Emitter e = emitters.Array[i];
                            if (aMod.Shape.Intersects(e.Bounds)) {
                                e.AddAreaModule(aMod);
                                aMod.AddEmitter(e);
                            } else {
                                e.RemoveAreaModule(aMod);
                                aMod.RemoveEmitter(e);
                            }
                        }
                    }

                    // Update emitters.
                    for (int i = 0; i < visibleEmitters.Count; i++) {
                        visibleEmitters.Array[i].Update(deltaTime);
                    }
                } break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void FindVisible(Span<Vector4> viewBounds)
        {
            visibleEmitters.Clear();
            if (viewBounds == default) {
                visibleEmitters.AddRange(emitters);
            } else {
                for (int i = 0; i < emitters.Count; i++) {
                    Emitter emitter = emitters.Array[i];
                    if (emitter.Enabled && CheckIntersection(emitter.Bounds, viewBounds)) {
                        visibleEmitters.Add(emitter);
                        emitter.IsVisible = true;
                    } else {
                        emitter.Clear();
                        emitter.IsVisible = false;
                    }
                }
            }
        }

        private static void EndThreads()
        {
            foreach (ParticleThread pt in threads) {
                pt.End();
            }
            threads = null;
        }

        private static void CreateThreads()
        {
            threads = new QuickList<ParticleThread>();
            int threadsNum = Math.Max(Environment.ProcessorCount - 1, 1);
            for (int i = 0; i < threadsNum; i++) {
                threads.Add(new ParticleThread());
            }
        }

        private static Exception CheckForThreadException()
        {
            if(UseThreadPool)
                return null;

            foreach (ParticleThread pt in threads) {
                if (pt.State == ParticleThread.ThreadState.Exception) {
                    return pt.Exception;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the list of emitters. 
        /// This is not a safe copy - do not modify it directly!
        /// </summary>
        /// <returns>List of internal particle emitters.</returns>
        public static QuickList<Emitter> GetEmitters()
            => emitters;

        public static void GetEmitters(BlendMode blendMode, QuickList<Emitter> existing, SearchFlags search = SearchFlags.None)
            => ContainerManager.Get(blendMode, existing, search);

        public static void GetEmitters(byte layer, QuickList<Emitter> existing, SearchFlags search = SearchFlags.None)
            => ContainerManager.Get(layer, existing, search);

        public static QuickList<Emitter> GetEmitters((byte, BlendMode) key)
            => ContainerManager.Get(key);

        public static void GetEmitters((byte, BlendMode) key, QuickList<Emitter> existing, SearchFlags search = SearchFlags.None)
            => ContainerManager.Get(key, existing, search);

        public static QuickList<Emitter> GetVisibleEmitters()
            => visibleEmitters;

        public static EmitterContainer GetContainer((byte, BlendMode) key)
            => ContainerManager.GetContainer(key);

        public static void GetContainers(QuickList<EmitterContainer> existing)
            => ContainerManager.GetContainers(existing);

        private static void CreateTasks(float deltaTime)
        {
            if (UseThreadPool) {
                updateTask = Task.Factory.StartNew(() => {
                    QuickParallel.ForEach(areaModules, aMod => {
                        foreach (Emitter e in emitters) {
                            if (aMod.Shape.Intersects(e.Bounds)) {
                                e.AddAreaModule(aMod);
                                aMod.AddEmitter(e);
                            } else {
                                e.RemoveAreaModule(aMod);
                                aMod.RemoveEmitter(e);
                            }
                        }
                    });
                }).ContinueWith(t1 => {
                    QuickParallel.ForEach(visibleEmitters, (emitters, count) => {
                        for (int i = 0; i < count; i++) {
                            emitters[i].Update(deltaTime);
                        }
                    });
                });
            } else {
                foreach (AreaModule aMod in areaModules) {
                    foreach (Emitter e in emitters) {
                        if (aMod.Shape.Intersects(e.Bounds)) {
                            e.AddAreaModule(aMod);
                            aMod.AddEmitter(e);
                        } else {
                            e.RemoveAreaModule(aMod);
                            aMod.RemoveEmitter(e);
                        }
                    }
                }

                // TODO: A better thread allocation method. Implement complexity values / load balancing.
                // TODO: ALSO moving the game window, while the fps is high, causes the particle engine to deadlock. wtf.
                
                foreach (ParticleThread pt in threads) {
                    pt.NewFrame(deltaTime);
                }

                int curThread = 0;
                foreach (Emitter e in emitters) {
                    threads.Array[curThread].AssignWork(e);

                    curThread++;
                    if(curThread == threads.Count) {
                        curThread = 0;
                    }
                }

                foreach (ParticleThread pt in threads) {
                    pt.Start();
                }
            }
        }


        private static void DestroyPending(float deltaTime)
        {
            Emitter[] pendingDestroyArray = pendingDestroy.Array;
            for (int i = pendingDestroy.Count - 1; i >= 0; i--) {
                Emitter emitter = pendingDestroyArray[i];
                emitter.TimeToLive -= deltaTime;
                if (emitter.TimeToLive <= 0.0f) {
                    emitter.DisposeInternal();
                }
            }
        }

        /// <summary>
        /// Finalizes any update function currently in progress. This should be called before rendering.
        /// </summary>
        public static void WaitForThreads()
        {
            if (!Initialized)
                throw new InvalidOperationException("Particle engine has not been initialized. Call ParticleEngine.Initialize() first.");

            if (UseThreadPool) {
                if (updateTask != null && !updateTask.IsCompleted) {
                    updateTask.Wait();
                }
            } else {
                Exception e = CheckForThreadException();
                if (e != null) {
                    throw new Exception("An exception occured within a particle thread.", e);
                }

                int numFinished = 0;
                while (true) {
                    if (threads.Array[numFinished].State == ParticleThread.ThreadState.Idle) {
                        numFinished++;
                    }
                    if (numFinished == threads.Count) {
                        break;
                    }
                }
            }
        }

        internal static void AddEmitter(Emitter emitter)
        {
            if(emitter.ParticleEngineIndex != -1)
                return;

            emitter.ParticleEngineIndex = emitters.Count;
            emitters.Add(emitter);
            ContainerManager.Add(emitter);
            foreach (AreaModule aModule in emitter.GetAreaModules()) {
                aModule.AddEmitter(emitter);
            }
        }

        internal static void RemoveEmitter(Emitter emitter)
        {
            if(emitter.ParticleEngineIndex == -1)
                return;

            emitters.Remove(emitter);
            emitter.ParticleEngineIndex = -1;
            ContainerManager.Remove(emitter);
            foreach (AreaModule aModule in emitter.GetAreaModules()) {
                aModule.RemoveEmitter(emitter);
            }
            pendingDestroy.Remove(emitter);
        }

        internal static void AddAreaModule(AreaModule module)
        {
            if(module.AddedToEngine)
                return;

            module.AddedToEngine = true;
            areaModules.Add(module);
            foreach (Emitter e in module.GetEmitters()) {
                e.AddAreaModule(module);
            }
        }

        internal static void RemoveAreaModule(AreaModule module)
        {
            module.AddedToEngine = false;
            areaModules.Remove(module);
            foreach (Emitter e in module.GetEmitters()) {
                e.RemoveAreaModule(module);
            }
        }

        internal static void DestroyPending(Emitter emitter)
        {
            pendingDestroy.Add(emitter);
        }

        private static bool CheckIntersection(Vector4 bounds, Span<Vector4> otherBounds)
        {
            for (int i = 0; i < otherBounds.Length; i++) {
                if (bounds.Intersects(otherBounds[i])) {
                    return true;
                }
            }
            return false;
        }

        private static class ContainerManager
        {
            private static Dictionary<(byte, BlendMode), EmitterContainer> sortedEmitters = new Dictionary<(byte, BlendMode), EmitterContainer>();

            internal static void Add(Emitter emitter)
            {
                (byte, BlendMode) key = emitter.GetKey();
                if (sortedEmitters.TryGetValue(key, out EmitterContainer emitters)) {
                    emitters.Add(emitter);
                } else {
                    EmitterContainer e = new EmitterContainer(key, emitter);
                    sortedEmitters.Add(key, e);
                }
            }

            internal static void Remove(Emitter emitter)
            {
                (byte, BlendMode) key = emitter.GetKey();
                if (sortedEmitters.TryGetValue(key, out EmitterContainer emitters)) {
                    emitters.Remove(emitter);
                }
            }

            internal static void Clear()
            {
                foreach (var pair in sortedEmitters) {
                    pair.Value.Clear();
                }
            }

            internal static QuickList<Emitter> Get((byte, BlendMode) key)
                => sortedEmitters.TryGetValue(key, out EmitterContainer emitters) ? emitters.Emitters : null;

            internal static void Get((byte, BlendMode) key, QuickList<Emitter> existing, SearchFlags search)
            {
                if(sortedEmitters.TryGetValue(key, out EmitterContainer container)) {
                    container.Retrieve(existing, search);
                }
            }

            internal static void Get(byte layer, QuickList<Emitter> existing, SearchFlags search = SearchFlags.None)
            {
                foreach (var pair in sortedEmitters) {
                    EmitterContainer container = pair.Value;
                    if (!container.IsActive || container.Layer != layer) 
                        continue;

                    container.Retrieve(existing, search);
                }
            }

            internal static void Get(BlendMode blendMode, QuickList<Emitter> existing, SearchFlags search = SearchFlags.None)
            {
                foreach (var pair in sortedEmitters) {
                    EmitterContainer container = pair.Value;
                    if (!container.IsActive || container.BlendMode != blendMode) 
                        continue;

                    container.Retrieve(existing, search);
                }
            }

            internal static EmitterContainer GetContainer((byte, BlendMode) key) 
                => sortedEmitters.TryGetValue(key, out EmitterContainer emitters) ? emitters : null;

            internal static void GetContainers(QuickList<EmitterContainer> existing)
            {
                foreach (var pair in sortedEmitters) {
                    EmitterContainer container = pair.Value;
                    if (container.IsActive) {
                        existing.Add(container);
                    }
                }
            }
        }
    }

    public class EmitterContainer
    {
        public BlendMode BlendMode { get; }
        public byte Layer { get; }
        public (byte, BlendMode) Key => (Layer, BlendMode);

        public QuickList<Emitter> Emitters { get; }

        public bool IsActive => Emitters.Count > 0;

        internal EmitterContainer((byte, BlendMode) key, Emitter emitter = null)
        {
            (Layer, BlendMode) = key;
            Emitters = new QuickList<Emitter>();
            if (emitter != null)
                Emitters.Add(emitter);
        }

        private static bool CheckFlags(SearchFlags flags, Emitter e)
        {
            if(flags == SearchFlags.None)
                return true;
            if ((flags & SearchFlags.Enabled) != 0 && !e.Enabled) 
                return false;
            if ((flags & SearchFlags.Visible) != 0 && !e.IsVisible)
                return false;
                
            return true;
        }

        public void Retrieve(QuickList<Emitter> existing, SearchFlags search = SearchFlags.None)
        {
            if (search == SearchFlags.None) {
                existing.AddRange(Emitters);
            } else {
                foreach (Emitter e in Emitters) {
                    if (CheckFlags(search, e)) {
                        existing.Add(e);
                    }
                }
            }
        }

        internal void Clear() 
            => Emitters.Clear();

        internal void Add(Emitter emitter) 
            => Emitters.Add(emitter);

        internal void Remove(Emitter emitter)
            => Emitters.Remove(emitter);
    }

    internal class ParticleThread
    {
        public ThreadState State { get; private set; }
        public Exception Exception { get; private set; }

        private Thread thread;
        private AutoResetEvent resetEvent = new AutoResetEvent(false);
        private QuickList<Emitter> emitters = new QuickList<Emitter>();
        private float deltaTime;

        public ParticleThread()
        {
            thread = new Thread(Run);
            thread.IsBackground = true;
            thread.Name = "Particle Thread";
            thread.Start();
        }

        public void Start()
        {
            if (emitters.Count > 0) {
                State = ThreadState.Running;
                resetEvent.Set();
            }
        }

        public void NewFrame(float deltaTime)
        {
            if (State != ThreadState.Idle) {
                throw new Exception("Particle thread isn't idle.");
            }

            this.deltaTime = deltaTime;
            emitters.Clear();
        }

        public void AssignWork(Emitter emitter)
        {
            if (State != ThreadState.Idle) {
                throw new Exception("Particle thread isn't idle.");
            }

            emitters.Add(emitter);
        }

        public void End()
        {
            State = ThreadState.Terminated;
            resetEvent.Set();
        }

        private void Run()
        {
            while (true) {
                resetEvent.WaitOne();
                if (State == ThreadState.Terminated || State == ThreadState.Exception)
                    break;

                try {
                    State = ThreadState.Running;
                    for (int i = 0; i < emitters.Count; i++) {
                        emitters.Array[i].Update(deltaTime);
                    }
                } catch (Exception e) {
                    State = ThreadState.Exception;
                    Exception = e;
                }

                if(State == ThreadState.Exception)
                    break;

                State = ThreadState.Idle;
                resetEvent.Reset();
            }
        }

        public enum ThreadState
        {
            Idle,
            Running,
            Exception,
            Terminated
        }
    }

    [Flags]
    public enum SearchFlags
    {
        None = 0,
        Visible = 1,
        Enabled = 2,
    }

    /// <summary>
    /// Controls how particles are allocated for new emitters.
    /// </summary>
    public enum ParticleAllocationMode
    {
        /// <summary>Particles are allocated using standard arrays. This should only be used if ParticleAllocationMode.ArrayPool causes
        ///          unintended behaviour.</summary>
        Array,

        /// <summary>Particles are allocated using the shared array pool.
        ///          This option will result in less garbage generation, and faster instantiation of new particle emitters.
        ///          Most useful when creating and destroying many particle emitters at runtime.</summary>
        ArrayPool
    }

    /// <summary>
    /// Controls how the particle engine updates.
    /// </summary>
    public enum UpdateMode
    {
        /// <summary>Update is done using Parallel loops, within tasks. ParticleEngine.WaitForThreads() must be called when the state
        ///          is to be synchronized. For example, ParticleEngine.WaitForThreads() would be called before you render or query the particles.
        ///          Results in better performance on machines with >2 cores.</summary>
        ParallelAsynchronous,

        /// <summary>Update is done synchronously using Parallel loops. Results in betters performance on machines with >2 cores.
        ///          State is synchronized immediately after Update() has finished processing.</summary>
        ParallelSynchronous,

        /// <summary>Update is done synchronously on a single thread. Results in lower performance on machines with
        ///          >2 cores, and potentially better performance on machines with 1-2 cores.</summary>
        Synchronous
    }

    /// <summary>
    /// Controls how the particle engine handles certain exceptions.
    /// </summary>
    public enum ErrorHandling
    {
        /// <summary>The Engine will prioritize stability, and will attempt to correct certain exceptions. Exceptions related to emitter
        ///          values, such as invalid bounds, will not be thrown. Exceptions that can't be corrected will still be thrown.</summary>
        Stability,

        /// <summary>Exceptions will be thrown whenever values are invalid. This is useful for debugging and fine-tuning particle emitters.</summary>
        Throw
    }
}
