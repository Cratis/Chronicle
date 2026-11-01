// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// Owns any delivery activity and its W3C propagation value.
/// </summary>
public sealed class DeliveryTraceContext : IDisposable
{
    readonly Activity? _activity;

    DeliveryTraceContext(Activity? activity, string traceParent)
    {
        _activity = activity;
        TraceParent = traceParent;
    }

    /// <summary>
    /// Gets the single traceparent value for this attempt.
    /// </summary>
    public string TraceParent { get; }

    /// <summary>
    /// Propagates the ambient W3C trace, or creates a delivery trace.
    /// </summary>
    /// <param name="correlationId">The batch's last event correlation.</param>
    /// <returns>The trace context, to dispose after sending.</returns>
    public static DeliveryTraceContext Create(CorrelationId correlationId)
    {
        if (Activity.Current is { IdFormat: ActivityIdFormat.W3C, Id: { } id }) return new(null, id);
        var activity = ChronicleActivity.Source.StartActivity("webhook.deliver", ActivityKind.Client);
        activity?.SetTag("chronicle.correlation_id", correlationId.Value.ToString());
        var parent = activity?.IdFormat == ActivityIdFormat.W3C ? activity.Id : null;
        return new(activity, parent ?? $"00-{ActivityTraceId.CreateRandom()}-{ActivitySpanId.CreateRandom()}-00");
    }

    /// <inheritdoc/>
    public void Dispose() => _activity?.Dispose();
}
