// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CS8019  // TODO: Orleans CodeGenerator is failing due to this: https://www.ingebrigtsen.info/2021/08/13/orleans-and-c-10-global-usings/
using System.Threading.Tasks;
using Orleans;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Defines the <see cref="IGrain"/> for dealing with <see cref="IEventLogObserver">observers</see>.
    /// </summary>
    public interface IEventLogObservers : IGrainWithGuidCompoundKey
    {
        /// <summary>
        /// Handle next <see cref="AppendedEvent"/>.
        /// </summary>
        /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
        Task Next(AppendedEvent @event);

        /// <summary>
        /// Add an <see cref="IEventLogObserver">observer</see> of the <see cref="IEventLog"/>.
        /// </summary>
        /// <param name="observer"><see cref="IEventLogObserver"/> to subscribe.</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        Task Subscribe(IEventLogObserver observer);

        /// <summary>
        /// Unsubscribe an <see cref="IEventLogObserver">observer</see> of the <see cref="IEventLog"/>.
        /// </summary>
        /// <param name="observer"><see cref="IEventLogObserver"/> to unsubscribe.</param>
        /// <returns>Awaitable <see cref="Task"/></returns>
        Task Unsubscribe(IEventLogObserver observer);
    }
}
