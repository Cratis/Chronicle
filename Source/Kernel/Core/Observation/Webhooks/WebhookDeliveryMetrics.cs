// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Records target-level circuit health without observer, path, or event-store cardinality.
/// </summary>
internal static class WebhookDeliveryMetrics
{
    static readonly Meter _meter = new(WellKnown.MeterName);
    static readonly Counter<long> _circuitOpened = _meter.CreateCounter<long>("chronicle-webhook-circuit-opened", description: "Number of webhook circuit openings per target authority");
    static readonly Counter<long> _circuitRejected = _meter.CreateCounter<long>("chronicle-webhook-circuit-rejected", description: "Number of webhook deliveries deferred because the target circuit is open");

    /// <summary>
    /// Records a target circuit opening.
    /// </summary>
    /// <param name="authority">The normalized scheme, host, and port.</param>
    internal static void CircuitOpened(string authority) => _circuitOpened.Add(1, new KeyValuePair<string, object?>("authority", authority));

    /// <summary>
    /// Records a delivery rejected by an open target circuit.
    /// </summary>
    /// <param name="authority">The normalized scheme, host, and port.</param>
    internal static void CircuitRejected(string authority) => _circuitRejected.Add(1, new KeyValuePair<string, object?>("authority", authority));
}
