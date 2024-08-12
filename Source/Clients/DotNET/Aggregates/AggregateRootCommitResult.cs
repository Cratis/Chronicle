// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents the result of committing an <see cref="IAggregateRoot"/>.
/// </summary>
public class AggregateRootCommitResult
{
    /// <summary>
    /// Gets a value indicating whether or not the commit was successful.
    /// </summary>
    public IEnumerable<object> Events { get; init; } = [];

    /// <summary>
    /// Gets the constraint violations that occurred during the commit.
    /// </summary>
    public IEnumerable<ConstraintViolation> ConstraintViolations { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether or not the commit was successful.
    /// </summary>
    public bool IsSuccess => !ConstraintViolations.Any();

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    public IEnumerable<AppendError> Errors { get; init; } = [];

    /// <summary>
    /// Implicitly convert from <see cref="AggregateRootCommitResult"/> to <see cref="bool"/>.
    /// </summary>
    /// <param name="result">The <see cref="AggregateRootCommitResult"/>.</param>
    public static implicit operator bool(AggregateRootCommitResult result) => result.IsSuccess;

    /// <summary>
    /// Create a successful <see cref="AggregateRootCommitResult"/>.
    /// </summary>
    /// <param name="events">Collection of events.</param>
    /// <returns><see cref="AggregateRootCommitResult"/>.</returns>
    public static AggregateRootCommitResult Successful(IImmutableList<object>? events = default) => new() { Events = events ?? ImmutableList<object>.Empty };

    /// <summary>
    /// Create an <see cref="AggregateRootCommitResult"/> from <see cref="IUnitOfWork"/>.
    /// </summary>
    /// <param name="unitOfWork"><see cref="IUnitOfWork"/> to create from.</param>
    /// <returns>A new instance of <see cref="AggregateRootCommitResult"/>.</returns>
    public static AggregateRootCommitResult CreateFrom(IUnitOfWork unitOfWork)
    {
        return new AggregateRootCommitResult
        {
            Events = unitOfWork.GetEvents(),
            ConstraintViolations = unitOfWork.GetConstraintViolations(),
            Errors = unitOfWork.GetAppendErrors()
        };
    }
}
