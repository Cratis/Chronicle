// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Represents a persisted closed stream scope.
/// </summary>
/// <param name="Scope">The dimensions covered by the closure.</param>
/// <param name="Owner">The owner of the closure.</param>
/// <param name="SequenceNumber">The sequence number at closure, or unavailable for legacy rows.</param>
/// <param name="ClosedAt">The time of closure, or null for legacy rows.</param>
public record ClosedStream(ClosedStreamScope Scope, ClosedStreamOwner Owner, EventSequenceNumber SequenceNumber, DateTimeOffset? ClosedAt);
