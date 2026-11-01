// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_ObserverRegistrationPrecedence.when_evaluating;

public class and_one_of_several_owner_instances_is_newer : given.a_versioned_owner
{
    void Establish() => _owning = _owning with
    {
        Targets = [.. _owning.Targets, new(SiloAddress.Zero, new ConnectedClient { ClientType = ".NET", Version = "3.0.0" }), new(SiloAddress.Zero, new ConnectedClient { ClientType = "TypeScript", Version = "19.115.0" })]
    };

    void Because() => _result = ObserverRegistrationPrecedence.Evaluate(_incoming, _owning);

    [Fact] void should_keep_the_owner() => _result.Outcome.ShouldEqual(RegistrationPrecedenceOutcome.OwnerIsNewer);
    [Fact] void should_compare_with_the_highest_known_version() => _result.OwnerVersion.ShouldEqual("3.0.0");
}
