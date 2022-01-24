// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events.Schemas;
using Aksio.Cratis.Events.Store.EventLogs;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IObserver"/>.
    /// </summary>
    [StorageProvider(ProviderName = ObserverState.StorageProvider)]
    public class Observer : Grain<ObserverState>, IObserver
    {
        readonly ConcurrentDictionary<Guid, StreamSubscriptionHandle<AppendedEvent>> _subscriptions = new();
        readonly ISchemaStore _schemaStore;
        readonly IJsonComplianceManager _jsonComplianceManager;
        readonly IRequestContextManager _requestContextManager;
        readonly IConnectedClients _connectedObservers;
        IAsyncStream<AppendedEvent>? _stream;
        ObserverId _observerId = Guid.Empty;
        TenantId _tenantId = TenantId.NotSet;
        EventLogId _eventLogId = EventLogId.Unspecified;

        /// <summary>
        /// Initializes a new instance of the <see cref="Observer"/> class.
        /// </summary>
        /// <param name="schemaStore"><see cref="ISchemaStore"/> for event schemas.</param>
        /// <param name="jsonComplianceManager"><see cref="IJsonComplianceManager"/> for handling compliance on events.</param>
        /// <param name="requestContextManager"><see cref="IRequestContextManager"/> for working with the Orleans request context.</param>
        /// <param name="connectedClients"><see cref="IConnectedClients"/>.</param>
        public Observer(
            ISchemaStore schemaStore,
            IJsonComplianceManager jsonComplianceManager,
            IRequestContextManager requestContextManager,
            IConnectedClients connectedClients)
        {
            _schemaStore = schemaStore;
            _jsonComplianceManager = jsonComplianceManager;
            _requestContextManager = requestContextManager;
            _connectedObservers = connectedClients;
            _connectedObservers.ClientDisconnected += async (_) =>
            {
                foreach (var (subscriptionId, handler) in _subscriptions)
                {
                    await Unsubscribe(subscriptionId);
                }
            };
        }

        /// <inheritdoc/>
        public override async Task OnActivateAsync()
        {
            _observerId = this.GetPrimaryKey(out var eventLogIdAsString);
            var tenantIdAsString = _requestContextManager.Get(RequestContextKeys.TenantId)!.ToString();
            _tenantId = tenantIdAsString!;
            _eventLogId = eventLogIdAsString;

            var streamProvider = GetStreamProvider(EventLog.StreamProvider);
            _stream = streamProvider.GetStream<AppendedEvent>(_eventLogId, _tenantId.ToString());

            await base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task Subscribe(IEnumerable<EventType> eventTypes)
        {
            var connectionId = _requestContextManager.Get(RequestContextKeys.ConnectionId).ToString()!;
            var subscriptionHandle = await _stream!.SubscribeAsync(
                async (@event, _) =>
                {
                    if (!_connectedObservers.AnyConnectedClients && connectionId != ConnectionId.Kernel)
                    {
                        return;
                    }

                    var eventSchema = await _schemaStore.GetFor(@event.Metadata.Type.Id, @event.Metadata.Type.Generation);
                    var releasedContent = await _jsonComplianceManager.Release(eventSchema.Schema, @event.Context.EventSourceId, @event.Content);
                    var releasedEvent = new AppendedEvent(@event.Metadata, @event.Context, releasedContent);

                    var key = PartitionedObserverKeyHelper.Create(_tenantId, _eventLogId, @event.Context.EventSourceId);
                    var partitionedObserver = GrainFactory.GetGrain<IPartitionedObserver>(_observerId, keyExtension: key);
                    try
                    {
                        await partitionedObserver.SetConnectionId(connectionId);
                        await partitionedObserver.OnNext(releasedEvent, eventTypes);

                        State.Offset = @event.Metadata.SequenceNumber + 1;
                        State.LastHandled = @event.Metadata.SequenceNumber + 1;
                        await WriteStateAsync();
                    }
                    catch (Exception)
                    {
                        var i = 0;
                        i++;
                    }
                },
                new EventLogSequenceNumberTokenWithFilter(State.Offset, eventTypes));

            _subscriptions[subscriptionHandle.HandleId] = subscriptionHandle;
        }

        /// <inheritdoc/>
        public async Task Unsubscribe(Guid subscriptionId)
        {
            if (_subscriptions.ContainsKey(subscriptionId))
            {
                await _subscriptions[subscriptionId].UnsubscribeAsync();
                _subscriptions.Remove(subscriptionId, out _);
            }
        }
    }
}
