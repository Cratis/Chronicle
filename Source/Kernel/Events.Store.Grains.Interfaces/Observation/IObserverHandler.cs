// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Copyright (c) Cratis. All rights reserved.

using Orleans;

namespace Cratis.Events.Store.Grains.Observation
{
    /// <summary>
    /// Defines a handler
    /// </summary>
    public interface IObserverHandler : IGrainObserver
    {
        /// <summary>
        /// Method that gets called when a new <see cref="AppendedEvent"/> is ready to be handled.
        /// </summary>
        /// <param name="context">The <see cref="ObserverContext"/>.</param>
        /// <param name="event">The actual <see cref="AppendedEvent"/>.</param>
        void OnNext(ObserverContext context, AppendedEvent @event);
    }
}
