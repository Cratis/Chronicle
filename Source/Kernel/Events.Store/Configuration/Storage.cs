// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Configuration;

namespace Cratis.Events.Store.Configuration
{
    /// <summary>
    /// Represents the configuration for storage.
    /// </summary>
    [Configuration]
    public record Storage
    {
        /// <summary>
        /// Get the <see cref="EventStore"/> configuration object.
        /// </summary>
        public EventStore EventStore { get; init; } = new EventStore();
    };
}
