// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using System.Collections.Concurrent;
using Cratis.Events.Store.Observation;
using Cratis.Execution;
using Cratis.Extensions.Orleans.Execution;
using Orleans;
using Orleans.Streams;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IObserver"/>.
    /// </summary>
    public class Observer : Grain, IObserver
    {
        readonly ConcurrentDictionary<Guid, StreamSubscriptionHandle<AppendedEvent>> _subscriptions = new();
        readonly IRequestContextManager _requestContextManager;
        readonly IConnectedClients _connectedObservers;
        IAsyncStream<AppendedEvent>? _stream;
        ObserverId _observerId = Guid.Empty;
        TenantId _tenantId = TenantId.NotSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="Observer"/> class.
        /// </summary>
        /// <param name="requestContextManager"></param>
        /// <param name="connectedObservers"></param>
        public Observer(
            IRequestContextManager requestContextManager,
            IConnectedClients connectedObservers)
        {
            _requestContextManager = requestContextManager;
            _connectedObservers = connectedObservers;
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
            _observerId = this.GetPrimaryKey(out var eventLogId);
            var tenantIdAsString = _requestContextManager.Get(RequestContextKeys.TenantId)!.ToString();
            _tenantId = tenantIdAsString!;

            var streamProvider = GetStreamProvider(EventLog.StreamProvider);
            _stream = streamProvider.GetStream<AppendedEvent>(Guid.Parse(eventLogId), _tenantId.ToString());

            await base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task<Guid> Subscribe(IEnumerable<EventType> eventTypes)
        {
            var subscriptionHandle = await _stream!.SubscribeAsync(
                async (@event, _) =>
                {
                    if (_connectedObservers.AnyConnectedClients)
                    {
                        return;
                    }
                    var partitionedObserver = GrainFactory.GetGrain<IPartitionedObserver>(_observerId, keyExtension: @event.EventContext.EventSourceId);
                    await partitionedObserver.OnNext(@event);
                }, new EventTypeFilteredStreamSequenceToken(0, eventTypes));

            _subscriptions[subscriptionHandle.HandleId] = subscriptionHandle;

            return subscriptionHandle.HandleId;
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
