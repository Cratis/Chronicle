// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an inspected closed stream scope.
/// </summary>
/// <param name="Scope">The closed dimensions.</param>
/// <param name="Origin">The closure origin.</param>
/// <param name="ClosedBy">The owning constraint, or null for manual closures.</param>
/// <param name="SequenceNumber">The sequence number at closure, or unavailable for legacy rows.</param>
/// <param name="ClosedAt">The closing timestamp, or null for legacy rows.</param>
public record ClosedStream(ClosedStreamScope Scope, ClosedStreamOrigin Origin, ConstraintName? ClosedBy, EventSequenceNumber SequenceNumber, DateTimeOffset? ClosedAt);
