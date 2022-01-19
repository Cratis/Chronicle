// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Aksio.Cratis.Events.Store.Grains
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLogObservers"/>.
    /// </summary>
    public class EventLogObservers : Grain, IEventLogObservers
    {
        readonly List<IEventLogObserver> _subscriptions = new();

        /// <inheritdoc/>
        public Task Next(AppendedEvent @event)
        {
            // Thoughts:
            // - What if we have a Grain the an observer - identified by its observer identifier and the partition key
            // - We then get an instance of the child grain
            // - The child grain can then have IGrainObservers registered - these can be the actual client calls
            //     - We need to figure out if an exception can be caught..
            // - Child grain is then responsible for managing offset
            _subscriptions.ForEach(_ => _.Next(@event));
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Subscribe(IEventLogObserver observer)
        {
            // TODO: Look at : https://dotnet.github.io/orleans/docs/grains/observers.html - create reliable manager for clients
            _subscriptions.Add(observer);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task Unsubscribe(IEventLogObserver observer)
        {
            _subscriptions.Remove(observer);
            return Task.CompletedTask;
        }
    }
}
