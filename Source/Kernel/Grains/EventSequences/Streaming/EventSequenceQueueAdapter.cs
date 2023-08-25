// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.DependencyInversion;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IQueueAdapter"/> for MongoDB event log.
/// </summary>
public class EventSequenceQueueAdapter : IQueueAdapter
{
    readonly Dictionary<QueueId, EventSequenceQueueAdapterReceiver> _receivers = new();

    readonly IStreamQueueMapper _mapper;
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly ProviderFor<IIdentityStore> _identityStoreProvider;

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
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="identityStoreProvider">Provider for <see cref="IIdentityStore"/>.</param>
    public EventSequenceQueueAdapter(
        string name,
        IStreamQueueMapper mapper,
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        ProviderFor<IIdentityStore> identityStoreProvider)
    {
        Name = name;
        _mapper = mapper;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _identityStoreProvider = identityStoreProvider;
    }

    /// <inheritdoc/>
    public IQueueAdapterReceiver CreateReceiver(QueueId queueId) => CreateReceiverIfNotExists(queueId);

    /// <inheritdoc/>
    public async Task QueueMessageBatchAsync<T>(StreamId streamId, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
    {
        var queueId = _mapper.GetQueueForStream(streamId);
        CreateReceiverIfNotExists(queueId);
        if (!token.IsWarmUp())
        {
            events = events.ToArray();
            var appendedEvents = new List<AppendedEvent>();
            foreach (var @event in events)
            {
                var appendedEvent = (@event as AppendedEvent)!;
                try
                {
                    await _eventSequenceStorageProvider().Append(
                        streamId.GetKeyAsString(),
                        appendedEvent.Metadata.SequenceNumber,
                        appendedEvent.Context.EventSourceId,
                        appendedEvent.Metadata.Type,
                        appendedEvent.Context.Causation,
                        await _identityStoreProvider().GetFor(appendedEvent.Context.CausedBy),
                        DateTimeOffset.UtcNow,
                        appendedEvent.Context.ValidFrom,
                        appendedEvent.Content);

                    appendedEvents.Add(appendedEvent);
                }
                catch (Exception ex)
                {
                    // Make sure we put all successful events on the stream for any subscribers to get.
                    _receivers[queueId].AddAppendedEvent(streamId, appendedEvents, requestContext);
                    var microserviceAndTenant = MicroserviceAndTenant.Parse(streamId.GetNamespace()!);

                    throw new UnableToAppendToEventSequence(
                        streamId.GetKeyAsString(),
                        microserviceAndTenant.MicroserviceId,
                        microserviceAndTenant.TenantId,
                        appendedEvent.Metadata.SequenceNumber,
                        appendedEvent.Context.EventSourceId,
                        ex);
                }
            }
        }

        _receivers[queueId].AddAppendedEvent(streamId, events.Cast<AppendedEvent>().ToArray(), requestContext);
    }

    IQueueAdapterReceiver CreateReceiverIfNotExists(QueueId queueId)
    {
        if (_receivers.TryGetValue(queueId, out var receiver)) return receiver;

        receiver = new EventSequenceQueueAdapterReceiver();
        _receivers[queueId] = receiver;
        return receiver;
    }
}
