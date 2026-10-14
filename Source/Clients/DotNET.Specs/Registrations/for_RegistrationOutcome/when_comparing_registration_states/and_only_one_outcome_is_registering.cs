// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_RegistrationOutcome.when_comparing_registration_states;

public class and_only_one_outcome_is_registering : Specification
{
    RegistrationOutcome _previous;
    RegistrationOutcome _registering;
    bool _equal;

    void Establish()
    {
        _previous = new(true, []);
        _registering = _previous with { IsRegistering = true };
    }

    void Because() => _equal = _previous.Equals(_registering);

    [Fact] void should_distinguish_an_in_flight_run() => _equal.ShouldBeFalse();
    [Fact] void should_include_the_state_in_the_hash() => _previous.GetHashCode().ShouldNotEqual(_registering.GetHashCode());
    [Fact] void should_preserve_the_previous_success() => _registering.IsSuccess.ShouldBeTrue();
}
