// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Receivers;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Receivers;

/// <summary>
/// Resolves the target from the provisional receiver route, never from query values.
/// </summary>
/// <param name="targets">The configured store and namespace validation.</param>
/// <remarks>
/// Only the configured store and the namespace produced by the configured namespace resolver are accepted.
/// The client exposes no non-creating namespace lookup, so route values cannot select another namespace.
/// </remarks>
internal sealed class RouteValuesDeliveryTargetResolver(DeliveryTargetValidation targets) : IDeliveryTargetResolver
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

        return targets.Validate(new(type.Value, id, store, tenant));
    }
}
