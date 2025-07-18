// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency;

/// <summary>
/// Represents a violation of a concurrency scope.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> of the event source.</param>
/// <param name="ExpectedSequenceNumber">The expected <see cref="EventSequenceNumber"/> .</param>
/// <param name="ActualSequenceNumber">The actual <see cref="EventSequenceNumber"/>.</param>
public record ConcurrencyViolation(
    EventSourceId EventSourceId,
    EventSequenceNumber ExpectedSequenceNumber,
    EventSequenceNumber ActualSequenceNumber);
