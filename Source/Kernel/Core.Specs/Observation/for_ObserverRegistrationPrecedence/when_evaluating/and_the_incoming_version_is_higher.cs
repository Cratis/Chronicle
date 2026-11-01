// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.when_evaluating;

public class and_the_incoming_version_is_higher : given.a_versioned_owner
{
    void Because() => _result = ObserverRegistrationPrecedence.Evaluate(_incoming, _owning);

    [Fact] void should_prefer_the_incoming_registration() => _result.Outcome.ShouldEqual(RegistrationPrecedenceOutcome.IncomingIsNewer);
}
