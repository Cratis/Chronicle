// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies;

public class when_an_override_would_loosen_the_server_gate : given.generation_policies
{
    void Establish()
    {
        _options = new ChronicleOptions
        {
            Features = new() { ProjectionGenerations = true },
            Observers = new() { GenerationPromotion = PromotionGateMode.Operator }
        };
        _overrides.GetForEventStore(_eventStore).Returns(new GenerationPromotionPolicyOverride(Mode: PromotionGateMode.Deployment));
        _overrides.GetForProjection(_eventStore, _projectionId).Returns(new GenerationPromotionPolicyOverride(Mode: PromotionGateMode.OnCatchUp));
    }

    void Because() => _result = _policies.Resolve(_eventStore, _projectionId, false, false);

    [Fact] void should_preserve_the_server_ceiling() => _result.Mode.ShouldEqual(PromotionGateMode.Operator);
}
