// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using Orleans;
using Orleans.Streams;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IPartitionedObserver"/>.
    /// </summary>
    public class PartitionedObserver : Grain, IPartitionedObserver
    {
        IAsyncStream<AppendedEvent>? _stream;

        /// <inheritdoc/>
        public override async Task OnActivateAsync()
        {
            var streamProvider = GetStreamProvider("observer-handlers");
            _stream = streamProvider.GetStream<AppendedEvent>(Guid.Parse("4680f4dc-5731-4fde-9b3c-a0f59b7713d9"), null); //"f455c031-630e-450d-a75b-ca050c441708");

            await base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task<bool> OnNext(AppendedEvent @event)
        {
            try
            {
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
