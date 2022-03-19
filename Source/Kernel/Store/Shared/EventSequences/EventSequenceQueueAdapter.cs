// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.DependencyInversion;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventLogs;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapter"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapter : IQueueAdapter
{
    readonly ConcurrentDictionary<QueueId, EventLogQueueAdapterReceiver> _receivers = new();

    readonly IStreamQueueMapper _mapper;
    readonly ProviderFor<IEventSequences> _eventLogsProvider;

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public bool IsRewindable => true;

    /// <inheritdoc/>
    public StreamProviderDirection Direction => StreamProviderDirection.ReadWrite;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceQueueAdapter"/> class.
    /// </summary>
    /// <param name="name">Name of stream.</param>
    /// <param name="mapper"><see cref="IStreamQueueMapper"/> for getting queue identifiers.</param>
    /// <param name="eventLogsProvider">Provider for <see cref="IEventSequences"/>.</param>
    public EventSequenceQueueAdapter(
        string name,
        IStreamQueueMapper mapper,
        ProviderFor<IEventSequences> eventLogsProvider)
    {
        Name = name;
        _mapper = mapper;
        _eventLogsProvider = eventLogsProvider;
    }

    /// <inheritdoc/>
    public IQueueAdapterReceiver CreateReceiver(QueueId queueId)
    {
        var receiver = new EventLogQueueAdapterReceiver();
        _receivers[queueId] = receiver;
        return receiver;
    }

    /// <inheritdoc/>
    public async Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
    {
        var queueId = _mapper.GetQueueForStream(streamGuid, streamNamespace);
        if (token.SequenceNumber != -1)
        {
            foreach (var @event in events)
            {
                var appendedEvent = (@event as AppendedEvent)!;
                try
                {
                    await _eventLogsProvider().Append(streamGuid, appendedEvent.Metadata.SequenceNumber, appendedEvent.Context.EventSourceId, appendedEvent.Metadata.Type, appendedEvent.Content);
                }
                catch (Exception ex)
                {
                    throw new UnableToAppendToEventSequence(streamGuid, streamNamespace, appendedEvent.Metadata.SequenceNumber, appendedEvent.Context.EventSourceId, ex);
                }
            }
        }

        _receivers[queueId].AddAppendedEvent(streamGuid, events.Cast<AppendedEvent>().ToArray(), requestContext);
    }
}
