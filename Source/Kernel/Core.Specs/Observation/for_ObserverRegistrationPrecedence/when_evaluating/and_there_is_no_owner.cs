// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.when_evaluating;

public class and_there_is_no_owner : given.a_versioned_owner
{
    void Because() => _result = ObserverRegistrationPrecedence.Evaluate(_incoming, null);

    [Fact] void should_have_no_conflict() => _result.Outcome.ShouldEqual(RegistrationPrecedenceOutcome.NoConflict);
}
