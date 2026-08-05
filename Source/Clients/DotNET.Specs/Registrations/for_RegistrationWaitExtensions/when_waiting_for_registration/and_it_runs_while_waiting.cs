// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationWaitExtensions.when_waiting_for_registration;

public class and_it_runs_while_waiting : given.an_event_store_whose_registration_is_observed
{
    const int ObservationsBeforeItRuns = 3;

    RegistrationOutcome _result;

    protected override RegistrationOutcome Observe()
    {
        _timesObserved++;
        return _timesObserved < ObservationsBeforeItRuns ? RegistrationOutcome.NotRun : _ran;
    }

    async Task Because() => _result = await _eventStore.WaitForRegistration(TimeSpan.FromSeconds(5));

    [Fact] void should_return_the_outcome_it_ended_up_with() => _result.ShouldEqual(_ran);
    [Fact] void should_keep_asking_until_it_had_run() => _timesObserved.ShouldEqual(ObservationsBeforeItRuns);
}
