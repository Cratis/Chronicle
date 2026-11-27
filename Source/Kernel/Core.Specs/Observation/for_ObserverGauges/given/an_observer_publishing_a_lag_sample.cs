// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.given;

public class an_observer_publishing_a_lag_sample : the_observer_gauges
{
    public const string DistanceInstrument = "cratis.chronicle.observer.lag_distance";
    public const string StalledInstrument = "cratis.chronicle.observer.stalled";

    protected static readonly TimeSpan _validity = TimeSpan.FromMinutes(2);

    protected ObserverGaugeEntry _entry;

    protected DateTimeOffset SampledAt => _clock.GetUtcNow();

    void Establish() => _entry = _gauges.Register(_observerKey, ObserverType.Reactor);

    protected ObserverLagSample Sample(ObserverLagStatus status, ulong distance) =>
        new(status, ObserverLagCause.Unknown, 100UL, 100UL - distance + 1, 5UL, null, distance, SampledAt, SampledAt + _validity);

    protected IReadOnlyList<RecordedMeasurement> CollectLag() => [.. Collect().Where(_ => _.Instrument == DistanceInstrument || _.Instrument == StalledInstrument)];

    protected void Publish(ObserverLagSample? sample, ObserverRunningState state = ObserverRunningState.Active) =>
        _entry.Publish(new(state, 0, 0) { LagSample = sample });
}
