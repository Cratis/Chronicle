// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;

public class EventSequenceCacheForSpecs(
    IEventSequenceStorage eventSequenceStorage,
    ILogger<EventSequenceCache> logger) : EventSequenceCache(eventSequenceStorage, logger)
{
    public IEnumerable<AppendedEvent> Events => _eventsBySequenceNumber.Select(_ => _.Value.Event);
    public CachedAppendedEvent HeadEvent => _head!;
    public CachedAppendedEvent TailEvent => _tail!;
    public Dictionary<EventSequenceNumber, CachedAppendedEvent> EventsBySequenceNumber => _eventsBySequenceNumber;
}
