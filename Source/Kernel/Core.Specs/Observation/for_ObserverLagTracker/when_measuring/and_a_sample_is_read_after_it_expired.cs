// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverLagTracker.when_measuring;

public class and_a_sample_is_read_after_it_expired : given.a_tracker
{
    ObserverLagSample _sample;
    ObserverLagSample _result;

    void Establish() => _sample = Measure(0, 10);

    void Because() => _result = _sample.At(Start.AddMinutes(10));

    [Fact] void should_say_nothing_can_be_said() => _result.Status.ShouldEqual(ObserverLagStatus.Unmeasurable);
    [Fact] void should_not_keep_a_distance() => _result.Distance.ShouldBeNull();
    [Fact] void should_keep_when_it_was_taken() => _result.SampledAt.ShouldEqual(_sample.SampledAt);
}
