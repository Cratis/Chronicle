// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents a single histogram bucket for an event sequence.
/// </summary>
/// <param name="EventSequenceNumber">The lowest <see cref="EventSequenceNumber"/> in the bucket.</param>
/// <param name="Occurred">The earliest <see cref="DateTimeOffset"/> within the bucket.</param>
/// <param name="Count">Number of events contained in the bucket.</param>
public record HistogramBucket(
    EventSequenceNumber EventSequenceNumber,
    DateTimeOffset Occurred,
    long Count);
