// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Configuration;
using Orleans;

namespace Cratis.Events.Store.Grains
{
    /// <summary>
    /// Defines the configuration service for the event store.
    /// </summary>
    public interface IEventStoreConfiguration : IGrainWithGuidKey
    {
        /// <summary>
        /// Get current configuration.
        /// </summary>
        /// <returns>Configuration.</returns>
        Task<Storage> Get();
    }
}
