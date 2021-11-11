// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Extensions.Dolittle.EventStore
{
    /// <summary>
    /// Defines a system for working with the Dolittle event store.
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Get a specific <see cref="IEventStream"/>.
        /// </summary>
        /// <param name="id"><see cref="EventStreamId"/>.</param>
        /// <returns><see cref="IEventStream"/>.</returns>
        IEventStream GetStream(EventStreamId id);
    }
}
