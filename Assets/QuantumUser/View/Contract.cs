using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace QuantumUser.View
{
    public static class Contract
    {
        public const bool PreconditionCheckEnabledDefault = true;

        [Conditional("DEBUG")]
        public static void Require(bool condition, string explanation, [CallerArgumentExpression(nameof(condition))] string? expression = null)
        {
            if (!condition)
                throw new PreconditionException($"[{expression}] {explanation}");
        }

        [Conditional("DEBUG")]
        public static void RequireNotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? valueName = null) where T : class
        {
            if (value is null)
                throw new PreconditionException($"{valueName} must not be null");
        }

        [Conditional("DEBUG")]
        public static void RequireNotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? valueName = null) where T : struct
        {
            if (!value.HasValue)
                throw new PreconditionException($"{valueName} must not be null");
        }

        [Conditional("DEBUG")]
        public static void Ensure(bool condition, string explanation, [CallerArgumentExpression(nameof(condition))] string? expression = null)
        {
            if (!condition)
                throw new PostconditionException($"[{expression}] {explanation}");
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