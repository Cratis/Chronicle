// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Receivers;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Receivers;

/// <summary>
/// Resolves the target from the provisional receiver route, never from query values.
/// </summary>
internal sealed class RouteValuesDeliveryTargetResolver : IDeliveryTargetResolver
{
    /// <inheritdoc/>
    public DeliveryTarget? Resolve(HttpContext context)
    {
        var values = context.Request.RouteValues;
        var store = values[ChronicleReceiverRoute.EventStore]?.ToString();
        var tenant = values[ChronicleReceiverRoute.Namespace]?.ToString();
        var id = values[ChronicleReceiverRoute.ObserverId]?.ToString();
        var type = values[ChronicleReceiverRoute.ObserverType]?.ToString()?.ToLowerInvariant() switch
        {
            "reactor" => ObserverType.Reactor,
            "reducer" => ObserverType.Reducer,
            _ => (ObserverType?)null
        };
        if (type is null || string.IsNullOrWhiteSpace(store) || string.IsNullOrWhiteSpace(tenant) || string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return new(type.Value, id, store, tenant);
    }
}
