// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Starts reducer spans with the selected naming convention.
/// </summary>
internal static class ReducerTraces
{
    static readonly ClientSpanDefinition _handle = new("client.reducer.handle", WellKnownClientSpanNames.ReducerHandle, ActivityKind.Consumer)
    {
        LegacyKeys = ["event_store_name", "namespace_name", "event_sequence_id", "reducer_id"],
        ConventionKeys = [WellKnownTelemetryAttributes.EventStoreName, WellKnownTelemetryAttributes.EventStoreNamespace, WellKnownTelemetryAttributes.EventSequenceId, WellKnownTelemetryAttributes.ObserverId],
        ObserverType = "reducer"
    };

    /// <summary>Starts a reducer handling span.</summary>
    /// <param name="source">The activity source.</param>
    /// <param name="context">The routing attributes.</param>
    /// <param name="names">The naming convention.</param>
    /// <returns>The activity scope.</returns>
    internal static IActivityScope<Reducers> Handle(this IActivitySource<Reducers> source, ObserverSpanContext context, ClientSpanNames names = ClientSpanNames.Legacy) =>
        ClientSpanCompatibility.Start(
            source,
            _handle,
            names,
            new(context.EventStoreName, context.NamespaceName, context.EventSequenceId, context.ObserverId));
}
