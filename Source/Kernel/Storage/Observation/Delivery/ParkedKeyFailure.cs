// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents immutable diagnostics for a parked key's last failed attempt.
/// </summary>
/// <param name="EventType">The event type, if known.</param>
/// <param name="Messages">Failure messages, copied at construction.</param>
/// <param name="StackTrace">The captured stack trace.</param>
/// <param name="LastAttempted">The last attempt time.</param>
public sealed record ParkedKeyFailure(EventTypeId? EventType, ImmutableArray<string> Messages, string StackTrace, DateTimeOffset LastAttempted)
{
    /// <summary>
    /// Gets the immutable failure messages.
    /// </summary>
    public ImmutableArray<string> Messages { get; } = DeliveryCollections.Copy(Messages);

    /// <inheritdoc/>
    public bool Equals(ParkedKeyFailure? other) => other is not null && EventType == other.EventType &&
        Messages.SequenceEqual(other.Messages) && StackTrace == other.StackTrace && LastAttempted == other.LastAttempted;

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(EventType, DeliveryCollections.Hash(Messages), StackTrace, LastAttempted);
}
