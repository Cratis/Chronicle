// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies;

public class when_no_overrides_are_stored : given.generation_policies
{
    void Establish() => _policies = new GenerationPolicies(Options.Create(_options), new GenerationPolicyOverrides());

    void Because() => _result = _policies.Resolve(_eventStore, _projectionId, false, false);

    [Fact] void should_use_the_enabled_server_defaults() => _result.ShouldEqual(new GenerationPromotionPolicy(PromotionGateMode.OnCatchUp, TimeSpan.FromHours(24), 1, TimeSpan.FromSeconds(60)));
}
