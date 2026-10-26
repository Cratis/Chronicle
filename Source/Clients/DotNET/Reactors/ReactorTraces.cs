// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;

namespace Cratis.Chronicle.Reactors;

/// <summary>Starts reactor spans with the selected naming convention.</summary>
internal static class ReactorTraces
{
    /// <summary>Starts a reactor handling span.</summary>
    /// <param name="source">The activity source.</param>
    /// <param name="context">The routing attributes.</param>
    /// <param name="names">The naming convention.</param>
    /// <returns>The activity scope.</returns>
    internal static IActivityScope<Reactors> Handle(this IActivitySource<Reactors> source, ObserverSpanContext context, ClientSpanNames names = ClientSpanNames.Legacy) =>
        ClientSpanCompatibility.Start(
            source,
            new("client.reactor.handle", WellKnownClientSpanNames.ReactorHandle, ActivityKind.Consumer),
            names,
            new("event_store_name", context.EventStoreName),
            new("namespace_name", context.NamespaceName),
            new("event_sequence_id", context.EventSequenceId),
            new("reactor_id", context.ObserverId));
}
