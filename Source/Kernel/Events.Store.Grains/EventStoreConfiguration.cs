// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Configuration;
using Orleans;

namespace Cratis.Events.Store.Grains
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventStoreConfiguration"/>.
    /// </summary>
    public class EventStoreConfiguration : Grain, IEventStoreConfiguration
    {
        readonly Storage _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreConfiguration"/> class.
        /// </summary>
        /// <param name="configuration"><see cref="Storage"/> configuration.</param>
        public EventStoreConfiguration(Storage configuration) => _configuration = configuration;

        /// <inheritdoc/>
        public Task<Storage> Get() => Task.FromResult(_configuration);
    }
}
