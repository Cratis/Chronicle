// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Events.Store.EventSequences;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserver"/>.
/// </summary>
/// <remarks>
/// This is a partial class. For structural, navigation and maintenance purposes, you'll find partial implementations
/// representing different aspects.
/// </remarks>
[StorageProvider(ProviderName = ObserverState.StorageProvider)]
public partial class Observer : Grain<ObserverState>, IObserver, IRemindable
{
    /// <summary>
    /// The name of the recover reminder.
    /// </summary>
    public const string RecoverReminder = "observer-failure-recovery";
    readonly IEventSequenceStorageProvider _eventSequenceStorageProvider;
    readonly ILogger<Observer> _logger;
    readonly Dictionary<EventSourceId, StreamSubscriptionHandle<AppendedEvent>> _streamSubscriptionsByEventSourceId = new();
    StreamSubscriptionHandle<AppendedEvent>? _streamSubscription;
    IAsyncStream<AppendedEvent>? _stream;
    ObserverId _observerId = Guid.Empty;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IGrainReminder? _recoverReminder;
    IEventSequence? _eventSequence;
    IStreamProvider? _observerStreamProvider;

    bool HasSubscribedObserver => State.CurrentNamespace != ObserverNamespace.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observer"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProvider"><see creF="IEventSequenceStorageProvider"/> for working with the underlying event sequence.</param>
    /// <param name="logger"><see cref="ILogger{T}"/> for logging.</param>
    public Observer(
        IEventSequenceStorageProvider eventSequenceStorageProvider,
        ILogger<Observer> logger)
    {
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _observerId = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverKey.Parse(keyAsString);
        _eventSequenceId = key.EventSequenceId;
        _microserviceId = key.MicroserviceId;
        _tenantId = key.TenantId;

        _eventSequence = GrainFactory.GetGrain<IEventSequence>(_eventSequenceId, new MicroserviceAndTenant(_microserviceId, _tenantId));

        _observerStreamProvider = GetStreamProvider(WellKnownProviders.ObserverHandlersStreamProvider);

        var streamProvider = GetStreamProvider(WellKnownProviders.EventSequenceStreamProvider);
        var microserviceAndTenant = new MicroserviceAndTenant(_microserviceId, _tenantId);
        _stream = streamProvider.GetStream<AppendedEvent>(_eventSequenceId, microserviceAndTenant);

        _recoverReminder = await GetReminder(RecoverReminder);
        await HandleReminderRegistration();
        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task SetMetadata(ObserverName name, ObserverType type)
    {
        State.Name = name;
        State.Type = type;

        await WriteStateAsync();
    }

    bool HasDefinitionChanged(IEnumerable<EventType> eventTypes) => !State.EventTypes.OrderBy(_ => _.Id.Value).SequenceEqual(eventTypes.OrderBy(_ => _.Id.Value));

    async Task HandleEventForPartitionedObserver(AppendedEvent @event, bool setLastHandled = false)
    {
        try
        {
            if (!HasSubscribedObserver)
            {
                return;
            }

            var stream = _observerStreamProvider!.GetStream<AppendedEvent>(_observerId, State.CurrentNamespace);
            await stream.OnNextAsync(@event);

            State.Offset = @event.Metadata.SequenceNumber + 1;

            if (setLastHandled)
            {
                State.LastHandled = @event.Metadata.SequenceNumber;
            }

            var nextSequenceNumber = await _eventSequence!.GetNextSequenceNumber();
            if (State.Offset == nextSequenceNumber)
            {
                State.RunningState = ObserverRunningState.Active;
                _logger.Active(_observerId, _microserviceId, _eventSequenceId, _tenantId);
            }
            await WriteStateAsync();
        }
        catch (Exception ex)
        {
            State.FailPartition(
                @event.Context.EventSourceId,
                @event.Metadata.SequenceNumber,
                GetMessagesFromException(ex),
                ex.StackTrace ?? string.Empty);

            await WriteStateAsync();
            await HandleReminderRegistration();
        }
    }

    async Task SubscribeStream(Func<AppendedEvent, Task> handler)
    {
        _streamSubscription = await _stream!.SubscribeAsync(
            (@event, _) => handler(@event),
            new EventSequenceNumberTokenWithFilter(State.Offset, State.EventTypes));

        // Note: Add a warm up event. The internals of Orleans will not do the producer / consumer handshake only after an event has gone through the
        // stream. Since our observers can perform replays & catch ups at startup, we can't wait till the first event appears.
        const long sequence = -1;
        var @event = new AppendedEvent(
            new(new EventSequenceNumber(unchecked((ulong)sequence)), new EventType(EventTypeId.Unknown, 1)),
            new(EventSourceId.Unspecified, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, _tenantId, CorrelationId.New(), CausationId.System, CausedBy.System, EventObservationState.Initial),
            new JsonObject());
        await _stream!.OnNextAsync(@event, new EventSequenceNumberToken());
    }

    async Task UnsubscribeStream()
    {
        if (_streamSubscription is not null)
        {
            await _streamSubscription.UnsubscribeAsync();
            _streamSubscription = null;
        }
    }

    string[] GetMessagesFromException(Exception ex)
    {
        var messages = new List<string>
                {
                    ex.Message
                };
        while (ex.InnerException != null)
        {
            messages.Insert(0, ex.InnerException.Message);
            ex = ex.InnerException;
        }
        return messages.ToArray();
    }
}
