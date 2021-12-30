// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using System.Text;
using Cratis.Events.Store.Observation;
using Orleans;
using Orleans.Providers;
using Orleans.Streams;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IPartitionedObserver"/>.
    /// </summary>
    [StorageProvider(ProviderName = PartitionedObserverState.StorageProvider)]
    public class PartitionedObserver : Grain<PartitionedObserverState>, IPartitionedObserver
    {
        ObserverId _observerId = ObserverId.Unspecified;
        IAsyncStream<AppendedEvent>? _stream;

        /// <inheritdoc/>
        public override async Task OnActivateAsync()
        {
            // Key extension holds the event source id - for now we discard it as we don't need the information here,
            // its just to used for now to have a composite key of the observer and each partitioned identified by the
            // event source id.
            _observerId = this.GetPrimaryKey(out var _);

            var streamProvider = GetStreamProvider("observer-handlers");
            _stream = streamProvider.GetStream<AppendedEvent>(_observerId, null);

            await base.OnActivateAsync();
        }

        /// <inheritdoc/>
        public async Task OnNext(AppendedEvent @event)
        {
            if (State.IsFailed)
            {
                return;
            }

            try
            {
                await _stream!.OnNextAsync(@event);
            }
            catch (Exception ex)
            {
                State.IsFailed = true;
                State.Occurred = DateTimeOffset.UtcNow;
                State.SequenceNumber = @event.Metadata.SequenceNumber;
                State.StackTrace = ex.StackTrace ?? string.Empty;

                var messages = new List<string>
                {
                    ex.Message
                };

                while (ex.InnerException != null)
                {
                    messages.Insert(0, ex.InnerException.Message);
                    ex = ex.InnerException;
                }

                State.Messages = messages.ToArray();

                // TODO: Add a reminder to try to recover from the failure
                await WriteStateAsync();
            }
        }
    }
}
