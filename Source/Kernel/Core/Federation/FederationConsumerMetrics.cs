// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The federation consumer instruments on the Chronicle meter. The only tags are the binding id and, for skips and errors, a fixed classification.
/// </summary>
[Singleton]
public sealed class FederationConsumerMetrics : IFederationConsumerMetrics, IDisposable
{
    readonly Meter _meter = new(WellKnown.MeterName);
    readonly Counter<long> _pulled;
    readonly Counter<long> _appended;
    readonly Counter<long> _skipped;
    readonly Counter<long> _errors;
    readonly Gauge<long> _lag;

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationConsumerMetrics"/> class.
    /// </summary>
    public FederationConsumerMetrics()
    {
        _pulled = _meter.CreateCounter<long>("chronicle.federation.consumer.events.pulled", "{event}", "Events pulled from a federation producer");
        _appended = _meter.CreateCounter<long>("chronicle.federation.consumer.events.appended", "{event}", "Federated events appended to the local inbox");
        _skipped = _meter.CreateCounter<long>("chronicle.federation.consumer.events.skipped", "{event}", "Pulled events not appended, by reason");
        _errors = _meter.CreateCounter<long>("chronicle.federation.consumer.errors", "{error}", "Failed federation pulls and appends");
        _lag = _meter.CreateGauge<long>("chronicle.federation.consumer.lag", "{event}", "Events a federation binding is behind its producer");
    }

    /// <inheritdoc/>
    public void EventsPulled(string bindingId, int count) => _pulled.Add(count, Binding(bindingId));

    /// <inheritdoc/>
    public void EventsAppended(string bindingId, int count) => _appended.Add(count, Binding(bindingId));

    /// <inheritdoc/>
    public void EventSkipped(string bindingId, string reason) =>
        _skipped.Add(1, Binding(bindingId), new KeyValuePair<string, object?>("reason", reason));

    /// <inheritdoc/>
    public void Error(string bindingId, string kind) =>
        _errors.Add(1, Binding(bindingId), new KeyValuePair<string, object?>("kind", kind));

    /// <inheritdoc/>
    public void Lag(string bindingId, ulong lag) => _lag.Record((long)Math.Min(lag, long.MaxValue), Binding(bindingId));

    /// <inheritdoc/>
    public void Dispose() => _meter.Dispose();

    static KeyValuePair<string, object?> Binding(string bindingId) => new("binding", bindingId);
}
