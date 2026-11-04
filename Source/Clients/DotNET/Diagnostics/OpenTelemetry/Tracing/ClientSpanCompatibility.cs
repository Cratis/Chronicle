// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>
/// Starts client spans using precomputed operation names and attribute keys.
/// </summary>
internal static class ClientSpanCompatibility
{
    /// <summary>
    /// Starts an operation only when its source has listeners.
    /// </summary>
    /// <typeparam name="T">The owner of the span.</typeparam>
    /// <param name="source">The activity source.</param>
    /// <param name="definition">The precomputed operation metadata.</param>
    /// <param name="names">The selected naming mode.</param>
    /// <param name="values">The operation's attribute values.</param>
    /// <returns>The activity scope, or a shared empty scope when nobody listens.</returns>
    internal static IActivityScope<T> Start<T>(IActivitySource<T> source, ClientSpanDefinition definition, ClientSpanNames names, ClientSpanValues values)
    {
        if (source?.ActualSource is not { } actualSource || !actualSource.HasListeners()) return EmptyScope<T>.Instance;

        return names == ClientSpanNames.Convention
            ? StartConvention<T>(actualSource, definition, values)
            : StartLegacy<T>(actualSource, definition, values);
    }

    static ActivityScope<T> StartLegacy<T>(ActivitySource source, ClientSpanDefinition definition, ClientSpanValues values)
    {
        var activity = source.StartActivity(definition.LegacyName, definition.Kind);
        values.SetTags(activity, definition.LegacyKeys);
        if (values.EventSourceId is { } id) activity?.SetTag("event_source_id", id);
        return new ActivityScope<T>(activity);
    }

    static ActivityScope<T> StartConvention<T>(ActivitySource source, ClientSpanDefinition definition, ClientSpanValues values)
    {
        var tags = values.SamplingTags(definition.ConventionKeys);
        if (definition.ObserverType is { } observerType) tags.Add(WellKnownTelemetryAttributes.ObserverType, observerType);
        var activity = source.StartActivity(definition.ConventionName, definition.Kind, parentContext: default, tags: tags);
        values.SetTags(activity, definition.LegacyKeys);
        if (values.EventSourceId is { } id)
        {
            activity?.SetTag("event_source_id", id);
            activity?.SetTag(WellKnownTelemetryAttributes.EventSourceId, id);
        }
        return new ActivityScope<T>(activity);
    }

    /// <summary>
    /// Holds one reusable empty scope per owner rather than allocating one at each disabled span start.
    /// </summary>
    /// <typeparam name="T">The owner of the span.</typeparam>
    static class EmptyScope<T>
    {
        /// <summary>
        /// The reusable empty scope.
        /// </summary>
        internal static readonly IActivityScope<T> Instance = new ActivityScope<T>(null);
    }
}
