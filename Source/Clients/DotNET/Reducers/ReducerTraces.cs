// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;

namespace Cratis.Chronicle.Reducers;

/// <summary>Starts reducer spans with the selected naming convention.</summary>
internal static class ReducerTraces
{
    /// <summary>Starts a reducer handling span.</summary>
    /// <param name="source">The activity source.</param>
    /// <param name="context">The routing attributes.</param>
    /// <param name="names">The naming convention.</param>
    /// <returns>The activity scope.</returns>
    internal static IActivityScope<Reducers> Handle(this IActivitySource<Reducers> source, ObserverSpanContext context, ClientSpanNames names = ClientSpanNames.Legacy) =>
        ClientSpanCompatibility.Start(source, new("client.reducer.handle", WellKnownClientSpanNames.ReducerHandle, ActivityKind.Consumer), names,
            new("event_store_name", context.EventStoreName), new("namespace_name", context.NamespaceName), new("event_sequence_id", context.EventSequenceId), new("reducer_id", context.ObserverId));
}
