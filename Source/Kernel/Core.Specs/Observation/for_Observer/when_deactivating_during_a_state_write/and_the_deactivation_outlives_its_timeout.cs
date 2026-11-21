// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_deactivating_during_a_state_write;

public class and_the_deactivation_outlives_its_timeout : given.an_observer_whose_state_write_can_be_held
{
    readonly CancellationTokenSource _timeout = new();
    Exception? _error;

    async Task Because()
    {
        var deactivation = await DeactivateWithTheWriteInFlight(
            new DeactivationReason(DeactivationReasonCode.ApplicationRequested, "Rolling upgrade"),
            _timeout.Token);

        // The runtime cancels the token, stops waiting and disposes the grain; the abandoned task is the only one left to see the outcome.
        await _timeout.CancelAsync();
        _observer.Dispose();
        _stateWrite.Release.SetResult();
        _error = await Catch.Exception(async () => await deactivation);
    }

    [Fact] void should_complete_without_leaving_an_exception_unobserved() => _error.ShouldBeNull();
}
