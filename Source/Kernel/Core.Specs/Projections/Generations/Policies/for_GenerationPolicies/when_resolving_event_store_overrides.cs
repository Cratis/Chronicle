// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies;

public class when_resolving_event_store_overrides : given.generation_policies
{
    void Establish() => _overrides.GetForEventStore(_eventStore).Returns(new GenerationPromotionPolicyOverride(
        Mode: PromotionGateMode.Deployment,
        RollbackWindow: TimeSpan.FromHours(6),
        MaxFollowingRetiredGenerations: 2,
        DeploymentConditionGracePeriod: TimeSpan.FromSeconds(30)));

    void Because() => _result = _policies.Resolve(_eventStore, _projectionId, false, false);

    [Fact] void should_apply_the_store_policy() => _result.ShouldEqual(new GenerationPromotionPolicy(PromotionGateMode.Deployment, TimeSpan.FromHours(6), 2, TimeSpan.FromSeconds(30)));
}
