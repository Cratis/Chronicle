// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.when_evaluating;

public class and_the_incoming_version_is_four_part : given.a_versioned_owner
{
    void Establish() => _incoming = _incoming with { Version = "1.0.0.0" };
    void Because() => _result = ObserverRegistrationPrecedence.Evaluate(_incoming, _owning);

    [Fact] void should_allow_last_wins() => _result.Outcome.ShouldEqual(RegistrationPrecedenceOutcome.Unordered);
}
