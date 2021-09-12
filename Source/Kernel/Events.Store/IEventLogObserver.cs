// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans;

namespace Cratis.Events.Store
{
    /// <summary>
    /// Defines an observer of <see cref="IEventLog"/>.
    /// </summary>
    public interface IEventLogObserver : IGrainObserver
    {
        /// <summary>
        /// Handle next <see cref="CommittedEvent"/>.
        /// </summary>
        /// <param name="event"><see cref="CommittedEvent"/> to handle.</param>
        void Next(CommittedEvent @event);
    }
}
