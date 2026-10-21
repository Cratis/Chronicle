// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationWaitExtensions.when_waiting_for_registration;

public class and_a_previous_success_is_registering_again : given.an_event_store_whose_registration_is_observed
{
    readonly RegistrationOutcome _newOutcome = new(true, []);
    RegistrationOutcome _result;

    protected override RegistrationOutcome Observe()
    {
        _timesObserved++;
        return _timesObserved == 1 ? _ran with { IsRegistering = true } : _newOutcome;
    }

    async Task Because() => _result = await _eventStore.WaitForRegistration(TimeSpan.FromSeconds(5));

    [Fact] void should_wait_for_the_new_run() => _timesObserved.ShouldEqual(2);
    [Fact] void should_return_the_new_outcome() => _result.ShouldBeSame(_newOutcome);
}
