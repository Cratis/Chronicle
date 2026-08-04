// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationWaitExtensions.when_waiting_for_registration;

public class and_it_has_already_run : given.an_event_store_whose_registration_is_observed
{
    RegistrationOutcome _result;

    async Task Because() => _result = await _eventStore.WaitForRegistration(TimeSpan.FromSeconds(5));

    [Fact] void should_return_the_outcome() => _result.ShouldEqual(_ran);
    [Fact] void should_not_wait_for_it() => _timesObserved.ShouldEqual(1);
}
