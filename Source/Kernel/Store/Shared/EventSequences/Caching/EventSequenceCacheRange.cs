// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Store.EventSequences.Caching;

/// <summary>
/// Represents a range in the <see cref="IEventSequenceCache"/> that indicates whether or not the range is in cache.
/// </summary>
/// <param name="Start">Start <see cref="EventSequenceNumber"/>.</param>
/// <param name="End">End <see cref="EventSequenceNumber"/>.</param>
public record EventSequenceCacheRange(EventSequenceNumber Start, EventSequenceNumber End);
