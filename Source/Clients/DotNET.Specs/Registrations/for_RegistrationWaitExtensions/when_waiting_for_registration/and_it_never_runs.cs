// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationWaitExtensions.when_waiting_for_registration;

public class and_it_never_runs : given.an_event_store_whose_registration_is_observed
{
    Exception _error;

    protected override RegistrationOutcome Observe()
    {
        _timesObserved++;
        return RegistrationOutcome.NotRun;
    }

    async Task Because() => _error = await Catch.Exception(() => _eventStore.WaitForRegistration(TimeSpan.FromMilliseconds(200)));

    [Fact] void should_give_up_at_the_timeout() => _error.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_have_asked_at_least_once() => _timesObserved.ShouldBeGreaterThan(0);
}
