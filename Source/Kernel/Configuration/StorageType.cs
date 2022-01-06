// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Configuration
{
    /// <summary>
    /// Represents the configuration for a specific storage type.
    /// </summary>
    public record StorageType
    {
        /// <summary>
        /// The type of storage used.
        /// </summary>
        public string Type { get; init; } = "Not Configured";

        /// <summary>
        /// The shared database connection configuration.
        /// </summary>
        public object Shared { get; init; } = "";

        /// <summary>
        /// The event store connection configuration per tenant.
        /// </summary>
        public IDictionary<string, object> EventStore { get; init; } = new Dictionary<string, object>();
    };
}
