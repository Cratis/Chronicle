// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.when_evaluating;

public class and_the_incoming_version_is_a_prerelease_of_the_owner : given.a_versioned_owner
{
    void Establish()
    {
        _incoming = _incoming with { Version = "2.0.0-rc.1" };
        _owning = _owning with { Targets = [new(SiloAddress.Zero, new ConnectedClient { ClientType = ".NET", Version = "2.0.0" })] };
    }

    void Because() => _result = ObserverRegistrationPrecedence.Evaluate(_incoming, _owning);

    [Fact] void should_keep_the_owner() => _result.Outcome.ShouldEqual(RegistrationPrecedenceOutcome.OwnerIsNewer);
}
