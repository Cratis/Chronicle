// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Defines the common properties for append result types.
/// </summary>
public interface IAppendResult
{
    /// <summary>
    /// Gets the <see cref="CorrelationId"/> for the operation.
    /// </summary>
    CorrelationId CorrelationId { get; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets whether or not there are any violations that occurred.
    /// </summary>
    bool HasConstraintViolations { get; }

    /// <summary>
    /// Gets whether or not there are any concurrency violations that occurred.
    /// </summary>
    bool HasConcurrencyViolations { get; }

    /// <summary>
    /// Gets whether or not there are any errors that occurred.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    /// Gets a value indicating whether the concurrency check was actually performed for the operation.
    /// </summary>
    /// <remarks>
    /// A skipped concurrency check looks from the outside exactly like a passing one - the append succeeds either
    /// way - so this is the only way for a caller to tell whether the serialization it believes it has was
    /// enforced. False means nothing was compared against the event store: the operation either asked for no check
    /// (<see cref="Concurrency.ConcurrencyScope.None"/>), carried no scope at all, or declared a scope with no
    /// expectation the kernel could validate
    /// (<see cref="Concurrency.ConcurrencyScope.IsIncomplete"/>).
    /// The first append into a scope reports false unless the first-append check is opted into - see
    /// <see cref="Concurrency.ConcurrencyOptions.CheckFirstAppendIntoAScope"/>.
    /// </remarks>
    /// <remarks>
    /// This has a default implementation returning <see langword="false"/> so that adding it does not break an existing
    /// implementation of this interface. An implementation that has not been updated therefore reports the check as
    /// not performed, which under-reports rather than promising a guarantee it cannot speak for.
    /// </remarks>
    bool ConcurrencyCheckPerformed => false;

    /// <summary>
    /// Gets any violations that occurred during the operation.
    /// </summary>
    IEnumerable<ConstraintViolation> ConstraintViolations { get; }

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    IEnumerable<AppendError> Errors { get; }
}
