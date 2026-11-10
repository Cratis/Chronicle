// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_gauges;

public class and_the_observer_deactivates : given.an_observer_reporting_gauges
{
    async Task Establish() => await _observer.TransitionTo<QuarantinedObserver>();

    Task Because() => _observer.OnDeactivateAsync(new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Spec"), default);

    [Fact] void should_have_registered_the_observer() => _registered.Count.ShouldEqual(1);
    [Fact] void should_release_what_it_registered() => _released.ShouldContainOnly(_registered);
}
