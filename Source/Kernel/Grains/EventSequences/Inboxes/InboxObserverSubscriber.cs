// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Json;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Schemas;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Inbox;

/// <summary>
/// Represents an implementation of <see cref="IInboxObserverSubscriber"/>.
/// </summary>
public class InboxObserverSubscriber : Grain, IInboxObserverSubscriber
{
    readonly ProviderFor<ISchemaStore> _schemaStoreProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ILogger<InboxObserverSubscriber> _logger;
    IEventSequence? _inboxEventSequence;
    ISchemaStore? _schemaStore;
    ISchemaStore? _sourceSchemaStore;
    MicroserviceId? _microserviceId;
    ObserverSubscriberKey? _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxObserverSubscriber"/> class.
    /// </summary>
    /// <param name="schemaStoreProvider">Provider for <see cref="ISchemaStore"/> for getting schema stores for other microservices.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
    /// <param name="logger">Logger for logging.</param>
    public InboxObserverSubscriber(
        ProviderFor<ISchemaStore> schemaStoreProvider,
        IExecutionContextManager executionContextManager,
        IExpandoObjectConverter expandoObjectConverter,
        ILogger<InboxObserverSubscriber> logger)
    {
        _schemaStoreProvider = schemaStoreProvider;
        _executionContextManager = executionContextManager;
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
            keyExtension: new MicroserviceAndTenant(_microserviceId, _key.TenantId));

        _executionContextManager.Establish(_microserviceId);
        _schemaStore = _schemaStoreProvider();

        _executionContextManager.Establish(_key.SourceMicroserviceId!);
        _sourceSchemaStore = _schemaStoreProvider();

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
                _executionContextManager.Establish(_key!.TenantId, @event.Context.CorrelationId, _microserviceId);

                EventSchema eventSchema;

                if (!await _schemaStore!.HasFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation))
                {
                    eventSchema = await _sourceSchemaStore!.GetFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation);
                    await _schemaStore.Register(eventSchema.Type, eventSchema.Schema.GetDisplayName(), eventSchema.Schema);
                }
                else
                {
                    eventSchema = await _schemaStore!.GetFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation);
                }

                _logger.ForwardingEvent(_key!.TenantId, _microserviceId!, @event.Metadata.Type.Id, eventSchema.Schema.GetDisplayName(), @event.Metadata.SequenceNumber);

                var content = _expandoObjectConverter.ToJsonObject(@event.Content, eventSchema.Schema);
                await _inboxEventSequence!.Append(@event.Context.EventSourceId, @event.Metadata.Type, content!);
                lastSuccessfullyObservedEvent = @event;
            }

            return ObserverSubscriberResult.Ok;
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
