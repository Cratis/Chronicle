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
    public IImmutableList<object> Events { get; init; } = ImmutableList<object>.Empty;

    /// <summary>
    /// Gets the constraint violations that occurred during the commit.
    /// </summary>
    public IImmutableList<ConstraintViolation> ConstraintViolations { get; init; } = ImmutableList<ConstraintViolation>.Empty;

    /// <summary>
    /// Gets a value indicating whether or not the commit was successful.
    /// </summary>
    public bool IsSuccess => ConstraintViolations.Count == 0;

    /// <summary>
    /// Gets any exception messages that might have occurred.
    /// </summary>
    public IImmutableList<AppendError> Errors { get; init; } = ImmutableList<AppendError>.Empty;

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
    public static AggregateRootCommitResult Successful(IImmutableList<object> events) => new() { Events = events };

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
