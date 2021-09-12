// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Configuration
{
    /// <summary>
    /// Represents the configuration for an event store.
    /// </summary>
    /// <param name="Type">The type of event store.</param>
    /// <param name="Configuration">The configuration per tenant.</param>
    public record EventStore(string Type, IDictionary<string, object> Configuration);
}
