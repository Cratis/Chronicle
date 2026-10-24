// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies;

public class when_resolving_a_kernel_owned_projection : given.generation_policies
{
    void Establish() => _options = new ChronicleOptions
    {
        Features = new() { ProjectionGenerations = true },
        Observers = new() { GenerationPromotion = PromotionGateMode.Gated }
    };

    void Because() => _result = _policies.Resolve(_eventStore, _projectionId, true, false, PromotionGateMode.Gated);

    [Fact] void should_promote_on_catch_up() => _result.Mode.ShouldEqual(PromotionGateMode.OnCatchUp);
    [Fact] void should_have_no_rollback_window() => _result.RollbackWindow.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_not_consult_store_overrides() => _overrides.DidNotReceive().GetForEventStore(Arg.Any<EventStoreName>());
    [Fact] void should_not_consult_projection_overrides() => _overrides.DidNotReceive().GetForProjection(Arg.Any<EventStoreName>(), Arg.Any<ProjectionId>());
}
