// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies;

public class when_a_client_requests_a_looser_gate : given.generation_policies
{
    void Establish() => _overrides.GetForProjection(_eventStore, _projectionId).Returns(new GenerationPromotionPolicyOverride(Mode: PromotionGateMode.Operator));

    void Because() => _result = _policies.Resolve(_eventStore, _projectionId, false, false, PromotionGateMode.Deployment);

    [Fact] void should_preserve_the_effective_gate() => _result.Mode.ShouldEqual(PromotionGateMode.Operator);
}
