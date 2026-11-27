// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_setup_failed_after_installing_the_target : given.an_observer_with_failed_client_restoration
{
    Task Because() => Restore();

    [Fact] void should_have_failed_after_installing_membership() => _firstError.ShouldEqual(_failure);
    [Fact] void should_have_retained_the_installed_subscription() => _installedAfterFailure.ShouldBeTrue();
    [Fact] void should_not_have_established_setup_after_failure() => _establishedAfterFailure.ShouldBeFalse();
    [Fact] async Task should_establish_the_subscription_on_retry() => (await _observer.IsSubscriptionEstablished()).ShouldBeTrue();
    [Fact] async Task should_keep_one_target() => (await _observer.GetSubscription()).Targets.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_next_position() => _stateStorage.State.NextEventSequenceNumber.Value.ShouldEqual(42UL);
    [Fact] void should_preserve_the_last_handled_position() => _stateStorage.State.LastHandledEventSequenceNumber.Value.ShouldEqual(41UL);
}
