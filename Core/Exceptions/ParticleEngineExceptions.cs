using System;

namespace SE.Core.Exceptions
{
    public class ParticleEngineInitializationException : Exception
    {
        public ParticleEngineInitializationException(string message = null) : base(message) { }
        public ParticleEngineInitializationException(string message, Exception innerException) : base(message, innerException) { }
        public ParticleEngineInitializationException(Exception innerException) : base(null, innerException) { }
    }

    public class EmitterValueException : Exception
    {
        public EmitterValueException(string message = null) : base(message) { }
        public EmitterValueException(string message, Exception innerException) : base(message, innerException) { }
        public EmitterValueException(Exception innerException) : base(null, innerException) { }
    }

    public class ParticleRendererException : Exception
    {
        public ParticleRendererException(string message = null) : base(message) { }
        public ParticleRendererException(string message, Exception innerException) : base(message, innerException) { }
        public ParticleRendererException(Exception innerException) : base(null, innerException) { }
    }

    public class InvalidEmitterValueException : Exception
    {
        public InvalidEmitterValueException(string msg) : base(msg) { }
        public InvalidEmitterValueException(Exception inner, string msg = null) : base(msg, inner) { }
    }
}
