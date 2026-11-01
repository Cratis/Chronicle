// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.when_evaluating;

public class and_the_incoming_client_is_not_dotnet : given.a_versioned_owner
{
    void Establish() => _incoming = _incoming with { ClientType = "TypeScript", Version = "19.115.0" };
    void Because() => _result = ObserverRegistrationPrecedence.Evaluate(_incoming, _owning);

    [Fact] void should_allow_last_wins() => _result.Outcome.ShouldEqual(RegistrationPrecedenceOutcome.Unordered);
}
