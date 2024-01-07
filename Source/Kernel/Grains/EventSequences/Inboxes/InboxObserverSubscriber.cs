// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.EventTypes;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Inbox;

/// <summary>
/// Represents an implementation of <see cref="IInboxObserverSubscriber"/>.
/// </summary>
public class InboxObserverSubscriber : Grain, IInboxObserverSubscriber
{
    readonly IStorage _storage;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ILogger<InboxObserverSubscriber> _logger;
    IEventSequence? _inboxEventSequence;
    IEventTypesStorage? _eventTypesStorage;
    IEventTypesStorage? _sourceEventTypesStorage;
    MicroserviceId? _microserviceId;
    ObserverSubscriberKey? _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxObserverSubscriber"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for working with underlying storage.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
    /// <param name="logger">Logger for logging.</param>
    public InboxObserverSubscriber(
        IStorage storage,
        IExpandoObjectConverter expandoObjectConverter,
        ILogger<InboxObserverSubscriber> logger)
    {
        _storage = storage;
        _expandoObjectConverter = expandoObjectConverter;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _microserviceId = this.GetPrimaryKey(out var keyAsString);
        _key = ObserverSubscriberKey.Parse(keyAsString);

        _inboxEventSequence = GrainFactory.GetGrain<IEventSequence>(
            EventSequenceId.Inbox,
            keyExtension: new EventSequenceKey(_microserviceId, _key.TenantId));

        _eventTypesStorage = _storage.GetEventStore((string)_microserviceId!).EventTypes;
        _sourceEventTypesStorage = _storage.GetEventStore((string)_key.SourceMicroserviceId!).EventTypes;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var currentEvent = events.First();
        AppendedEvent? lastSuccessfullyObservedEvent = default;

        try
        {
            foreach (var @event in events)
            {
                currentEvent = @event;
                EventTypeSchema eventSchema;

                if (!await _eventTypesStorage!.HasFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation))
                {
                    eventSchema = await _sourceEventTypesStorage!.GetFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation);
                    await _eventTypesStorage.Register(eventSchema.Type, eventSchema.Schema.GetDisplayName(), eventSchema.Schema);
                }
                else
                {
                    eventSchema = await _eventTypesStorage!.GetFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation);
                }

                _logger.ForwardingEvent(_key!.TenantId, _microserviceId!, @event.Metadata.Type.Id, eventSchema.Schema.GetDisplayName(), @event.Metadata.SequenceNumber);

                var content = _expandoObjectConverter.ToJsonObject(@event.Content, eventSchema.Schema);
                await _inboxEventSequence!.Append(@event.Context.EventSourceId, @event.Metadata.Type, content!, @event.Context.Causation, @event.Context.CausedBy);
                lastSuccessfullyObservedEvent = @event;
            }

            return ObserverSubscriberResult.Ok(events.Last().Metadata.SequenceNumber);
        }
        catch (Exception ex)
        {
            _logger.FailedForwardingEvent(
                _key!.TenantId,
                _microserviceId!,
                currentEvent.Metadata.Type.Id,
                currentEvent.Metadata.SequenceNumber,
                ex);

            return new(
                ObserverSubscriberState.Failed,
                lastSuccessfullyObservedEvent?.Metadata.SequenceNumber ?? EventSequenceNumber.Unavailable,
                ex.GetAllMessages(),
                ex.StackTrace ?? string.Empty);
        }
    }
}
