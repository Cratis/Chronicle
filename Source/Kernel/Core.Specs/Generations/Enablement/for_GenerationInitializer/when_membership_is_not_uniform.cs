// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement.for_GenerationInitializer;

public class when_membership_is_not_uniform : given.an_initializer
{
    void Establish() => _membership.Probe(Arg.Any<GenerationFloor>()).Returns(new GenerationMembershipEvidence(false, "membership changed"));

    async Task Because() => _result = await _initializer.Initialize(_name);

    [Fact] void should_refuse_enablement() => _result.ShouldBeOfExactType<GenerationInitializationBlocked>();
    [Fact] async Task should_keep_durable_membership_evidence() => (await _audit.GetBlockers()).Single().Kind.ShouldEqual(GenerationInitializationBlockerKind.MembershipNotUniform);
    [Fact] void should_not_adopt() => _catalog.Calls.ShouldEqual(0);
}
