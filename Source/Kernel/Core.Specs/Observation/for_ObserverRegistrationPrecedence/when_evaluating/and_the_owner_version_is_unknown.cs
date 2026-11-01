// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.when_evaluating;

public class and_the_owner_version_is_unknown : given.a_versioned_owner
{
    void Establish() => _owning = _owning with { Targets = [new(SiloAddress.Zero, new ConnectedClient { ClientType = ".NET", Version = string.Empty })] };
    void Because() => _result = ObserverRegistrationPrecedence.Evaluate(_incoming, _owning);

    [Fact] void should_allow_last_wins() => _result.Outcome.ShouldEqual(RegistrationPrecedenceOutcome.Unordered);
}
