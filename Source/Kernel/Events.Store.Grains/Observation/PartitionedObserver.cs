// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using Orleans;
using Orleans.Streams;
using Orleans.Streams.Core;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IPartitionedObserver"/>.
    /// </summary>
    public class PartitionedObserver : Grain, IPartitionedObserver
    {
        readonly IStreamSubscriptionManager _subscriptionManager;
        IAsyncStream<AppendedEvent>? _stream;

        public PartitionedObserver(IStreamSubscriptionManagerAdmin subscriptionManagerAdmin)
        {
            _subscriptionManager = subscriptionManagerAdmin.GetStreamSubscriptionManager(StreamSubscriptionManagerType.ExplicitSubscribeOnly);
        }


        public override async Task OnActivateAsync()
        {
            var id = this.GetPrimaryKey(out var tenantId);

            var streamProvider = GetStreamProvider("observer-handlers");
            _stream = streamProvider.GetStream<AppendedEvent>(Guid.Parse("4680f4dc-5731-4fde-9b3c-a0f59b7713d9"), null); //"f455c031-630e-450d-a75b-ca050c441708");

            await base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> OnNext(AppendedEvent @event)
        {
            var id = this.GetPrimaryKey(out var tenantId);

            var context = new ObserverContext(Guid.NewGuid(), Guid.NewGuid());
            //observer.OnNext(context, @event);

            try
            {
                //var subscribers =  await _stream!.GetAllSubscriptionHandles();
                var subscribers = await _subscriptionManager.GetSubscriptions("observer-handlers", new StreamIdentity(_stream!.Guid, _stream!.Namespace));

                await _stream!.OnNextAsync(@event);
            }
            catch (Exception)
            {
                var i = 0;
                i++;
            }

            return true;
        }
    }
}
