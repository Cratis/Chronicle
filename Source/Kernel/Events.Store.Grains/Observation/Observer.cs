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
        IAsyncStream<AppendedEvent>? _stream;
        ObserverId _observerId = Guid.Empty;
        TenantId _tenantId = TenantId.NotSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="Observer"/> class.
        /// </summary>
        /// <param name="requestContextManager"></param>
        public Observer(IRequestContextManager requestContextManager)
        {
            _requestContextManager = requestContextManager;
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
        public async Task<Guid> Subscribe(IEnumerable<EventType> eventTypes, IObserverHandler handler)
        {
            var subscriptionHandle = await _stream!.SubscribeAsync(
                async (@event, _) =>
                {
                    var partitionedObserver = GrainFactory.GetGrain<IPartitionedObserver>(_observerId, keyExtension: @event.EventContext.EventSourceId);
                    Console.WriteLine("*****************************");
                    Console.WriteLine("********** EVENT ************");
                    Console.WriteLine("*****************************");
                    await partitionedObserver.OnNext(handler, @event);
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
