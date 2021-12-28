// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using Orleans;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Represents an implementation of <see cref="IPartitionedObserver"/>.
    /// </summary>
    public class PartitionedObserver : Grain, IPartitionedObserver
    {
        /// <inheritdoc/>
        public Task<bool> OnNext(IObserverHandler observer, AppendedEvent @event)
        {
            var id = this.GetPrimaryKey(out var tenantId);

            var context = new ObserverContext(Guid.NewGuid(), Guid.NewGuid());
            observer.OnNext(context, @event);

            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public Task ReportStatus()
        {
            return Task.CompletedTask;
        }
    }
}
