// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies;

public class when_resolving_server_policy : given.generation_policies
{
    void Establish() => _options = new ChronicleOptions
    {
        Features = new() { ProjectionGenerations = true },
        Observers = new() { GenerationPromotion = PromotionGateMode.Deployment, DeploymentConditionGracePeriod = TimeSpan.FromSeconds(90) },
        ReadModels = new() { RollbackWindow = TimeSpan.FromHours(12), MaxFollowingRetiredGenerations = 2 }
    };

    void Because() => _result = _policies.Resolve(_eventStore, _projectionId, false, false);

    [Fact] void should_use_the_server_policy() => _result.ShouldEqual(new GenerationPromotionPolicy(PromotionGateMode.Deployment, TimeSpan.FromHours(12), 2, TimeSpan.FromSeconds(90)));
}
