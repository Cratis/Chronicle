// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_setup_fails_again : given.an_observer_with_failed_client_restoration
{
    Exception? _retryError;

    void Establish() => _failLookup = true;

    async Task Because() => _retryError = await Catch.Exception(Restore);

    [Fact] void should_propagate_the_second_failure() => _retryError.ShouldEqual(_failure);
    [Fact] async Task should_leave_setup_unestablished() => (await _observer.IsSubscriptionEstablished()).ShouldBeFalse();
    [Fact] async Task should_keep_one_target() => (await _observer.GetSubscription()).Targets.Count.ShouldEqual(1);
}
