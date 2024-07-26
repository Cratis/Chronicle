// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents the result of committing an <see cref="IAggregateRoot"/>.
/// </summary>
/// <param name="Success">Whether or not it was successful.</param>
/// <param name="Events">A collection of the events that was committed.</param>
public record AggregateRootCommitResult(bool Success, IImmutableList<object> Events)
{
    /// <summary>
    /// Implicitly convert from <see cref="AggregateRootCommitResult"/> to <see cref="bool"/>.
    /// </summary>
    /// <param name="result">The <see cref="AggregateRootCommitResult"/>.</param>
    public static implicit operator bool(AggregateRootCommitResult result) => result.Success;

    /// <summary>
    /// Create a successful <see cref="AggregateRootCommitResult"/>.
    /// </summary>
    /// <param name="events">Collection of events.</param>
    /// <returns><see cref="AggregateRootCommitResult"/>.</returns>
    public static AggregateRootCommitResult Successful(IImmutableList<object> events) => new(true, events);

    /// <summary>
    /// Create a failed <see cref="AggregateRootCommitResult"/>.
    /// </summary>
    /// <param name="events">Collection of events.</param>
    /// <returns><see cref="AggregateRootCommitResult"/>.</returns>
    public static AggregateRootCommitResult Failed(IImmutableList<object> events) => new(false, events);
}
