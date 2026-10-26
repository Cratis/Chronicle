// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;

namespace Cratis.Chronicle.EventSequences;

/// <summary>Starts event sequence spans with the selected naming convention.</summary>
internal static class EventSequenceTraces
{
    /// <summary>Starts a single append span.</summary>
    /// <param name="source">The activity source.</param>
    /// <param name="context">The routing attributes.</param>
    /// <param name="names">The naming convention.</param>
    /// <param name="tracedEventSourceId">The identifier after applying the exposure policy, or null when omitted.</param>
    /// <returns>The activity scope.</returns>
    internal static IActivityScope<EventSequence> Append(
        this IActivitySource<EventSequence> source,
        EventSequenceSpanContext context,
        ClientSpanNames names = ClientSpanNames.Legacy,
        string? tracedEventSourceId = null) =>
        ClientSpanCompatibility.Start(source, new("client.event_sequence.append", WellKnownClientSpanNames.EventSequenceAppend, ActivityKind.Client), names,
            new("event_store_name", context.EventStoreName), new("namespace_name", context.NamespaceName), new("event_sequence_id", context.EventSequenceId), new("event_source_type", context.EventSourceType), new("event_source_id", tracedEventSourceId));

    /// <summary>Starts a batch append span.</summary>
    /// <param name="source">The activity source.</param>
    /// <param name="context">The routing attributes.</param>
    /// <param name="names">The naming convention.</param>
    /// <returns>The activity scope.</returns>
    internal static IActivityScope<EventSequence> AppendMany(this IActivitySource<EventSequence> source, EventSequenceSpanContext context, ClientSpanNames names = ClientSpanNames.Legacy) =>
        ClientSpanCompatibility.Start(source, new("client.event_sequence.append_many", WellKnownClientSpanNames.EventSequenceAppendMany, ActivityKind.Client), names,
            new("event_store_name", context.EventStoreName), new("namespace_name", context.NamespaceName), new("event_sequence_id", context.EventSequenceId));
}
