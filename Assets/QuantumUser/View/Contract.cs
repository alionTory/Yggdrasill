using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace QuantumUser.View
{
    public static class Contract
    {
        public const bool PreconditionCheckEnabledDefault = true;

        [Conditional("DEBUG")]
        public static void Require(bool condition, string explanation)
        {
            if (!condition)
                throw new PreconditionException(explanation);
        }

        [Conditional("DEBUG")]
        public static void RequireNotNull<T>([NotNull] T? value, string explanation) where T : class
        {
            if (value is null)
                throw new PreconditionException(explanation);
        }

        [Conditional("DEBUG")]
        public static void RequireNotNull<T>([NotNull] T? value, string explanation) where T : struct
        {
            if (!value.HasValue)
                throw new PreconditionException(explanation);
        }

        [Conditional("DEBUG")]
        public static void Ensure(bool condition, string explanation)
        {
            if (!condition)
                throw new PostconditionException(explanation);
        }
    }

    public abstract class ContractException : Exception
    {
        protected ContractException() : base()
        {
        }

        protected ContractException(string message) : base(message)
        {
        }

        protected ContractException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ContractException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class PreconditionException : ContractException
    {
        public PreconditionException() : base()
        {
        }

        public PreconditionException(string message) : base(message)
        {
        }

        public PreconditionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public PreconditionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    public class PostconditionException : ContractException
    {
        public PostconditionException() : base()
        {
        }

        public PostconditionException(string message) : base(message)
        {
        }

        public PostconditionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public PostconditionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}