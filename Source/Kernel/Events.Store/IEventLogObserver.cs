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
        /// Handle next <see cref="AppendedEvent"/>.
        /// </summary>
        /// <param name="event"><see cref="AppendedEvent"/> to handle.</param>
        void Next(AppendedEvent @event);
    }
}
