// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_gauges;

public class and_the_observer_is_reset_for_development : given.an_observer_reporting_gauges
{
    async Task Establish() => await _observer.TransitionTo<QuarantinedObserver>();

    async Task Because()
    {
        await _observer.ResetForDevelopment();
        await _observer.PartitionFailed("late", 44UL, ["Late failure"], "Stack");
    }

    [Fact] void should_have_registered_the_observer_once() => _registered.Count.ShouldEqual(1);
    [Fact] void should_release_what_it_registered() => _released.ShouldContainOnly(_registered);
}
