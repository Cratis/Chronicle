// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_deactivating_during_a_state_write;

public class and_the_runtime_disposes_the_observer_before_the_write_finishes : given.an_observer_whose_state_write_can_be_held
{
    Exception? _error;
    int _writesFinishedBeforeTeardown;

    async Task Because()
    {
        var deactivation = await DeactivateWithTheWriteInFlight(
            new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Rolling upgrade"),
            CancellationToken.None);

        // The runtime stops waiting once the deactivation timeout cancels the token, then disposes the grain.
        _observer.Dispose();
        _stateWrite.Release.SetResult();
        _error = await Catch.Exception(async () => await deactivation);
        _writesFinishedBeforeTeardown = _stateWrite.Completed;
    }

    [Fact] void should_not_fail_the_deactivation() => _error.ShouldBeNull();
    [Fact] void should_let_the_write_in_flight_finish() => (_writesFinishedBeforeTeardown >= 1).ShouldBeTrue();
    [Fact] void should_not_write_again_once_disposed() => _stateWrite.Completed.ShouldEqual(_writesFinishedBeforeTeardown);
}
