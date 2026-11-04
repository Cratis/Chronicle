// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Traces;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Starts event sequence spans with the selected naming convention.
/// </summary>
internal static class EventSequenceTraces
{
    static readonly ClientSpanDefinition _append = new("client.event_sequence.append", WellKnownClientSpanNames.EventSequenceAppend, ActivityKind.Client)
    {
        LegacyKeys = ["event_store_name", "namespace_name", "event_sequence_id", "event_source_type"],
        ConventionKeys = [WellKnownTelemetryAttributes.EventStoreName, WellKnownTelemetryAttributes.EventStoreNamespace, WellKnownTelemetryAttributes.EventSequenceId, WellKnownTelemetryAttributes.EventSourceType]
    };

    static readonly ClientSpanDefinition _appendMany = new("client.event_sequence.append_many", WellKnownClientSpanNames.EventSequenceAppendMany, ActivityKind.Client)
    {
        LegacyKeys = ["event_store_name", "namespace_name", "event_sequence_id"],
        ConventionKeys = [WellKnownTelemetryAttributes.EventStoreName, WellKnownTelemetryAttributes.EventStoreNamespace, WellKnownTelemetryAttributes.EventSequenceId]
    };

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
        ClientSpanCompatibility.Start(
            source,
            _append,
            names,
            new(context.EventStoreName, context.NamespaceName, context.EventSequenceId, context.EventSourceType) { EventSourceId = tracedEventSourceId });

    /// <summary>Starts a batch append span.</summary>
    /// <param name="source">The activity source.</param>
    /// <param name="context">The routing attributes.</param>
    /// <param name="names">The naming convention.</param>
    /// <returns>The activity scope.</returns>
    internal static IActivityScope<EventSequence> AppendMany(this IActivitySource<EventSequence> source, EventSequenceSpanContext context, ClientSpanNames names = ClientSpanNames.Legacy) =>
        ClientSpanCompatibility.Start(
            source,
            _appendMany,
            names,
            new(context.EventStoreName, context.NamespaceName, context.EventSequenceId));
}
