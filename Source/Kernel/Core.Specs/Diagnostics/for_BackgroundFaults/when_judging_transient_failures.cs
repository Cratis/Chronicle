// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.for_BackgroundFaults;

public class when_judging_transient_failures : Specification
{
    bool _timeoutInsideAggregate;
    bool _timeoutWrapped;
    bool _mixedAggregate;
    bool _genuineFailure;

    void Because()
    {
        _timeoutInsideAggregate = BackgroundFaults.IsTransient(new AggregateException(new TimeoutException()));
        _timeoutWrapped = BackgroundFaults.IsTransient(new InvalidOperationException("outer", new TimeoutException()));
        _mixedAggregate = BackgroundFaults.IsTransient(new AggregateException(new TimeoutException(), new ArgumentException()));
        _genuineFailure = BackgroundFaults.IsTransient(new ArgumentException());
    }

    [Fact] void should_treat_an_aggregated_transient_failure_as_transient() => _timeoutInsideAggregate.ShouldBeTrue();
    [Fact] void should_treat_a_wrapped_transient_failure_as_transient() => _timeoutWrapped.ShouldBeTrue();
    [Fact] void should_not_treat_an_aggregate_with_a_genuine_failure_as_transient() => _mixedAggregate.ShouldBeFalse();
    [Fact] void should_not_treat_a_genuine_failure_as_transient() => _genuineFailure.ShouldBeFalse();
}
