// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>
/// Starts a single span with the selected name and both the existing and shared attribute keys.
/// </summary>
internal static class ClientSpanCompatibility
{
    static readonly Dictionary<string, string> _attributes = new Dictionary<string, string>
    {
        ["event_store_name"] = WellKnownTelemetryAttributes.EventStoreName,
        ["namespace_name"] = WellKnownTelemetryAttributes.EventStoreNamespace,
        ["event_sequence_id"] = WellKnownTelemetryAttributes.EventSequenceId,
        ["event_source_type"] = WellKnownTelemetryAttributes.EventSourceType,
        ["event_source_id"] = WellKnownTelemetryAttributes.EventSourceId,
        ["correlation_id"] = WellKnownTelemetryAttributes.CorrelationId,
        ["reactor_id"] = WellKnownTelemetryAttributes.ObserverId,
        ["reducer_id"] = WellKnownTelemetryAttributes.ObserverId
    };

    /// <summary>
    /// Selects the operation name before sampling so legacy mode preserves both the operation and display names.
    /// </summary>
    /// <typeparam name="T">The owner of the span.</typeparam>
    /// <param name="source">The activity source.</param>
    /// <param name="definition">The names and kind of the operation.</param>
    /// <param name="names">The selected naming mode.</param>
    /// <param name="tags">The existing attribute keys and values.</param>
    /// <returns>The activity scope.</returns>
    internal static IActivityScope<T> Start<T>(IActivitySource<T> source, ClientSpanDefinition definition, ClientSpanNames names, params KeyValuePair<string, object?>[] tags)
    {
        var attributes = new ActivityTagsCollection();
        foreach (var (key, value) in tags)
        {
            if (value is null) continue;
            if (_attributes.TryGetValue(key, out var shared)) attributes[shared] = value;
            if (key == "reactor_id") attributes[WellKnownTelemetryAttributes.ObserverType] = "reactor";
            if (key == "reducer_id") attributes[WellKnownTelemetryAttributes.ObserverType] = "reducer";
        }

        var name = names == ClientSpanNames.Convention ? definition.ConventionName : definition.LegacyName;
        var activity = source?.ActualSource?.StartActivity(name, definition.Kind, parentContext: default, tags: attributes);
        foreach (var (key, value) in tags) activity?.SetTag(key, value);
        return new ActivityScope<T>(activity);
    }
}
