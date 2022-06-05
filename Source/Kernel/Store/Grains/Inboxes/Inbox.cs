// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Grains.Observation;
using Aksio.Cratis.Events.Store.Inboxes;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Orleans;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Inboxes;

/// <summary>
/// Represents an implementation of <see cref="IInbox"/>.
/// </summary>
public class Inbox : Grain, IInbox
{
    readonly IExecutionContextManager _executionContextManager;
    IObserver? _observer;
    IEventSequence? _inboxEventSequence;
    MicroserviceId? _microserviceId;
    InboxKey? _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="Inbox"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/>.</param>
    public Inbox(IExecutionContextManager executionContextManager)
    {
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _microserviceId = this.GetPrimaryKey(out var keyAsString);
        _key = InboxKey.Parse(keyAsString);

        _inboxEventSequence = GrainFactory.GetGrain<IEventSequence>(
            EventSequenceId.Inbox,
            keyExtension: new MicroserviceAndTenant(_microserviceId, _key.TenantId));

        _observer = GrainFactory.GetGrain<IObserver>(
            _microserviceId,
            new ObserverKey(
                _microserviceId,
                _key.TenantId,
                EventSequenceId.Outbox,
                _key.MicroserviceId,
                _key.TenantId));

        var observerNamespace = new ObserverNamespace($"{_microserviceId}+${keyAsString}");
        var streamProvider = GetStreamProvider(WellKnownProviders.ObserverHandlersStreamProvider);
        var stream = streamProvider.GetStream<AppendedEvent>(_microserviceId, observerNamespace);
        await stream.SubscribeAsync(HandleEvent);

        await _observer.SetMetadata($"Inbox for ${_microserviceId}, Outbox from ${_key.MicroserviceId} for Tenant ${_key.TenantId}", ObserverType.Inbox);
        await _observer.Subscribe(Array.Empty<EventType>(), observerNamespace);
    }

    /// <inheritdoc/>
    public Task Start() => Task.CompletedTask;

    async Task HandleEvent(AppendedEvent @event, StreamSequenceToken token)
    {
        _executionContextManager.Establish(_key!.TenantId, @event.Context.CorrelationId, _microserviceId!);
        await _inboxEventSequence!.Append(@event.Context.EventSourceId, @event.Metadata.Type, @event.Content);
    }
}
