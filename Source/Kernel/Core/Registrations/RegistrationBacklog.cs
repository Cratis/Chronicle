// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Diagnostics;
using Cratis.DependencyInjection;
using Cratis.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// Represents an implementation of <see cref="IRegistrationBacklog"/> that publishes the registration backlog gauges.
/// </summary>
/// <remarks>
/// The instruments are new and have no legacy name, so they are published under the convention names whatever
/// Telemetry:MetricNames selects. They report per event store; a store that has drained keeps reporting zero until
/// the silo stops holding its registrations.
/// </remarks>
[Singleton]
public sealed class RegistrationBacklog : IRegistrationBacklog
{
    readonly ConcurrentDictionary<string, (int Pending, DateTimeOffset? OldestAcceptedAt)> _stores = new();
    readonly TimeProvider _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationBacklog"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="IMeter{T}"/> owning the instruments.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/>. Defaults to the system clock.</param>
    public RegistrationBacklog([FromKeyedServices(WellKnown.MeterName)] IMeter<RegistrationBacklog> meter, TimeProvider? timeProvider = null)
        : this(meter.ActualMeter, timeProvider ?? TimeProvider.System)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegistrationBacklog"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="Meter"/> owning the instruments, if any.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
    internal RegistrationBacklog(Meter? meter, TimeProvider timeProvider)
    {
        _clock = timeProvider;
        if (meter is null)
        {
            return;
        }

        meter.CreateObservableGauge(
            WellKnownMetricNames.RegistrationPending,
            ObservePending,
            WellKnownMetricNames.RegistrationPendingUnit,
            "Number of registrations the kernel has accepted and not applied yet, per event store");
        meter.CreateObservableGauge(
            WellKnownMetricNames.RegistrationOldestPendingAge,
            ObserveOldestPendingAge,
            WellKnownMetricNames.RegistrationOldestPendingAgeUnit,
            "How long the oldest accepted registration that has not been applied has been waiting, per event store");
    }

    /// <inheritdoc/>
    public void Report(EventStoreName eventStore, int pending, DateTimeOffset? oldestAcceptedAt) =>
        _stores[eventStore.Value] = (pending, pending == 0 ? null : oldestAcceptedAt);

    /// <inheritdoc/>
    public void Forget(EventStoreName eventStore) => _stores.TryRemove(eventStore.Value, out _);

    /// <summary>
    /// Observes how many registrations wait per event store.
    /// </summary>
    /// <returns>One measurement per event store.</returns>
    internal IEnumerable<Measurement<int>> ObservePending() =>
        [.. _stores.Select(_ => new Measurement<int>(_.Value.Pending, Tags(_.Key)))];

    /// <summary>
    /// Observes how long the oldest waiting registration has waited per event store.
    /// </summary>
    /// <returns>One measurement per event store, zero when nothing waits.</returns>
    internal IEnumerable<Measurement<double>> ObserveOldestPendingAge()
    {
        var now = _clock.GetUtcNow();
        return [.. _stores.Select(_ => new Measurement<double>(
            _.Value.OldestAcceptedAt is { } accepted ? Math.Max(0, (now - accepted).TotalSeconds) : 0,
            Tags(_.Key)))];
    }

    static KeyValuePair<string, object?> Tags(string eventStore) => new(WellKnownTelemetryAttributes.EventStoreName, eventStore);
}
