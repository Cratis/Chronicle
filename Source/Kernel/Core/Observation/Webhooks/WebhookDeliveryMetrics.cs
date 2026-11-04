// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Records target-level circuit health without observer, path, or event-store cardinality.
/// </summary>
internal sealed class WebhookDeliveryMetrics
{
    static readonly Meter _meter = new(WellKnown.MeterName);
    readonly IMetricInstrument<long> _circuitOpened;
    readonly IMetricInstrument<long> _circuitRejected;

    /// <summary>Initializes a new instance of the <see cref="WebhookDeliveryMetrics"/> class.</summary>
    /// <param name="names">The naming mode.</param>
    internal WebhookDeliveryMetrics(MetricNames names)
    {
        var metrics = new MetricSet(_meter, names);
        _circuitOpened = metrics.Counter<long>(WellKnownMetricNames.WebhookCircuitOpenings, WellKnownMetricNames.WebhookCircuitOpeningsUnit, "Number of webhook circuit openings per target authority", WellKnownTelemetryAttributes.WebhookAuthority);
        _circuitRejected = metrics.Counter<long>(WellKnownMetricNames.WebhookCircuitRejections, WellKnownMetricNames.WebhookCircuitRejectionsUnit, "Number of webhook deliveries deferred because the target circuit is open", WellKnownTelemetryAttributes.WebhookAuthority);
    }

    /// <summary>Records a target circuit opening.</summary>
    /// <param name="authority">The normalized scheme, host, and port.</param>
    internal void CircuitOpened(string authority) => _circuitOpened.Record(1, authority);

    /// <summary>Records a delivery rejected by an open target circuit.</summary>
    /// <param name="authority">The normalized scheme, host, and port.</param>
    internal void CircuitRejected(string authority) => _circuitRejected.Record(1, authority);
}
