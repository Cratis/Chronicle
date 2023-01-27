// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Events;
using Aksio.Cratis.Execution;
using Orleans.Providers.Streams.Common;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.EventSequences;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCacheDataAdapter"/>.
/// </summary>
public class EventSequenceCacheDataAdapter : IEventSequenceCacheDataAdapter
{
    record CacheSegment(IEnumerable<AppendedEvent> Events, MicroserviceId MicroserviceId, TenantId TenantId, IDictionary<string, object> RequestContext);

    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCacheDataAdapter"/> class.
    /// </summary>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    public EventSequenceCacheDataAdapter(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public IBatchContainer GetBatchContainer(ref CachedMessage cachedMessage)
    {
        var segment = JsonSerializer.Deserialize<CacheSegment>(cachedMessage.Segment, _jsonSerializerOptions)!;
        return new EventSequenceBatchContainer(
            segment.Events,
            cachedMessage.StreamGuid,
            segment.MicroserviceId,
            segment.TenantId,
            segment.RequestContext);
    }

    /// <inheritdoc/>
    public CachedMessage GetCachedMessage(IBatchContainer batchContainer)
    {
        var eventSequenceBatchContainer = (batchContainer as EventSequenceBatchContainer)!;
        var events = eventSequenceBatchContainer.GetEvents<AppendedEvent>().Select(_ => _.Item1).ToArray().AsEnumerable();
        var segment = new CacheSegment(
            events,
            eventSequenceBatchContainer.MicroserviceId,
            eventSequenceBatchContainer.TenantId,
            eventSequenceBatchContainer.AssociatedRequestContext);

        var segmentAsBytes = JsonSerializer.SerializeToUtf8Bytes(segment, _jsonSerializerOptions);
        return new CachedMessage
        {
            StreamGuid = batchContainer.StreamGuid,
            StreamNamespace = batchContainer.StreamNamespace,
            SequenceNumber = batchContainer.SequenceToken.SequenceNumber,
            EventIndex = 0,
            EnqueueTimeUtc = new DateTime((events.FirstOrDefault()?.Context.Occurred ?? DateTimeOffset.UtcNow).ToUnixTimeMilliseconds()),
            DequeueTimeUtc = DateTime.UtcNow,
            Segment = segmentAsBytes
        };
    }

    /// <inheritdoc/>
    public StreamSequenceToken GetSequenceToken(ref CachedMessage cachedMessage) => new EventSequenceNumberToken((ulong)cachedMessage.SequenceNumber);
}
