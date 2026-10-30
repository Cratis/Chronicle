// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies;

public class when_generations_are_disabled : given.generation_policies
{
    void Establish() => _options = new ChronicleOptions
    {
        Observers = new() { GenerationPromotion = PromotionGateMode.Gated },
        ReadModels = new() { RollbackWindow = TimeSpan.FromDays(2) }
    };

    void Because() => _result = _policies.Resolve(_eventStore, _projectionId, false, false, PromotionGateMode.Gated);

    [Fact] void should_return_an_inert_policy() => _result.ShouldEqual(new GenerationPromotionPolicy(PromotionGateMode.OnCatchUp, TimeSpan.Zero, 0, TimeSpan.Zero));
    [Fact] void should_not_consult_overrides() => _overrides.ReceivedCalls().ShouldBeEmpty();
}
