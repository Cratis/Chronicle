// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents a concurrency violation that occurred during an append operation.
/// </summary>
/// <param name="ExpectedEventSequenceNumber">The expected <see cref="EventSequenceNumber"/>.</param>
/// <param name="ActualEventSequenceNumber">The actual <see cref="EventSequenceNumber"/>.</param>
public record ConcurrencyViolation(EventSequenceNumber ExpectedEventSequenceNumber, EventSequenceNumber ActualEventSequenceNumber);
