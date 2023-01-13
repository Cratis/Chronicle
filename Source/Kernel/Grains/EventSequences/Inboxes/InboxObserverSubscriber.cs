// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Json;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Schemas;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Inbox;

/// <summary>
/// Represents an implementation of <see cref="IInboxObserverSubscriber"/>.
/// </summary>
public class InboxObserverSubscriber : Grain, IInboxObserverSubscriber
{
    readonly ISchemaStore _schemaStore;
    readonly ProviderFor<ISchemaStore> _sourceSchemaStoreProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly ILogger<InboxObserverSubscriber> _logger;
    IEventSequence? _inboxEventSequence;
    ISchemaStore? _sourceSchemaStore;
    MicroserviceId? _microserviceId;
    ObserverSubscriberKey? _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxObserverSubscriber"/> class.
    /// </summary>
    /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
    /// <param name="sourceSchemaStoreProvider">Provider for <see cref="ISchemaStore"/> for getting schema stores for other microservices.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between json and expando object.</param>
    /// <param name="logger">Logger for logging.</param>
    public InboxObserverSubscriber(
        ISchemaStore schemaStore,
        ProviderFor<ISchemaStore> sourceSchemaStoreProvider,
        IExecutionContextManager executionContextManager,
        IExpandoObjectConverter expandoObjectConverter,
        ILogger<InboxObserverSubscriber> logger)
    {
        _schemaStore = schemaStore;
        _sourceSchemaStoreProvider = sourceSchemaStoreProvider;
        _executionContextManager = executionContextManager;
        _expandoObjectConverter = expandoObjectConverter;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        _microserviceId = this.GetPrimaryKey(out var keyAsString);
        _key = ObserverSubscriberKey.Parse(keyAsString);

        _inboxEventSequence = GrainFactory.GetGrain<IEventSequence>(
            EventSequenceId.Inbox,
            keyExtension: new MicroserviceAndTenant(_microserviceId, _key.TenantId));

        _executionContextManager.Establish(_key.MicroserviceId);
        _sourceSchemaStore = _sourceSchemaStoreProvider();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task OnNext(AppendedEvent @event)
    {
        _executionContextManager.Establish(_key!.TenantId, @event.Context.CorrelationId, _microserviceId);

        EventSchema eventSchema;

        if (!await _schemaStore.HasFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation) && _sourceSchemaStore is not null)
        {
            eventSchema = await _sourceSchemaStore.GetFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation);
            await _schemaStore.Register(eventSchema.Type, eventSchema.Schema.GetDisplayName(), eventSchema.Schema);
        }
        else
        {
            eventSchema = await _schemaStore.GetFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation);
        }

        _logger.ForwardingEvent(_key!.TenantId, _microserviceId!, @event.Metadata.Type.Id, eventSchema.Schema.GetDisplayName(), @event.Metadata.SequenceNumber);

        var content = _expandoObjectConverter.ToJsonObject(@event.Content, eventSchema.Schema);
        await _inboxEventSequence!.Append(@event.Context.EventSourceId, @event.Metadata.Type, content!);
    }
}
