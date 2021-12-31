// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Grains;

namespace Cratis.Events
{
    /// <summary>
    /// Defines the store that holds events.
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Gets the default <see cref="IEventLog"/>.
        /// </summary>
        IClientEventLog EventLog { get; }
    }
}
