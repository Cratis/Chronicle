// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.when_evaluating;

public class and_the_owner_is_not_served_by_a_connected_client : given.a_versioned_owner
{
    void Establish() => _owning = _owning with { Targets = [] };
    void Because() => _result = ObserverRegistrationPrecedence.Evaluate(_incoming, _owning);

    [Fact] void should_have_no_conflict() => _result.Outcome.ShouldEqual(RegistrationPrecedenceOutcome.NoConflict);
}
