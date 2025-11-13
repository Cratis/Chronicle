// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
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
    /// Gets any violations that occurred during the operation.
    /// </summary>
    IEnumerable<ConstraintViolation> ConstraintViolations { get; }

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    IEnumerable<AppendError> Errors { get; }
}
