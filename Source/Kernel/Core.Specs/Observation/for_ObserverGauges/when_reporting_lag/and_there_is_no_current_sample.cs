// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_ObserverGauges.when_reporting_lag;

public class and_there_is_no_current_sample : given.an_observer_publishing_a_lag_sample
{
    IReadOnlyList<RecordedMeasurement> _none;
    IReadOnlyList<RecordedMeasurement> _unmeasurable;
    IReadOnlyList<RecordedMeasurement> _notActive;

    void Because()
    {
        _none = CollectLag();

        Publish(Sample(ObserverLagStatus.Lagging, 3));
        Publish(ObserverLagSample.Unmeasurable(ObserverLagCause.TailUnavailable, SampledAt, SampledAt + _validity));
        _unmeasurable = CollectLag();

        Publish(Sample(ObserverLagStatus.Lagging, 3), ObserverRunningState.Disconnected);
        _notActive = CollectLag();
    }

    [Fact] void should_report_nothing_for_an_observer_that_was_never_sampled() => _none.ShouldBeEmpty();
    [Fact] void should_report_nothing_for_an_unmeasurable_sample_after_a_measurable_one() => _unmeasurable.ShouldBeEmpty();
    [Fact] void should_report_nothing_while_the_observer_is_not_active() => _notActive.ShouldBeEmpty();
}
