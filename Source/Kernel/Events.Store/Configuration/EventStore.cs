// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Store.Configuration
{
    /// <summary>
    /// Represents the configuration for an event store.
    /// </summary>
    public record EventStore
    {
        /// <summary>
        /// The type of event store.
        /// </summary>
        public string Type { get; init; } = "Not Configured";

        /// <summary>
        /// The common event store connection configuration.
        /// </summary>
        public object Common { get; init; } = "";

        /// <summary>
        /// The event store connection configuration per tenant.
        /// </summary>
        public IDictionary<string, object> Configuration { get; init; } = new Dictionary<string, object>();
    }
}
