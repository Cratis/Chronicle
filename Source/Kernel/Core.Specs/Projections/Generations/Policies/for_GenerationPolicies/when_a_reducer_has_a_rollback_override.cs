// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies;

public class when_a_reducer_has_a_rollback_override : given.generation_policies
{
    void Establish() => _overrides.GetForProjection(_eventStore, _projectionId).Returns(new GenerationPromotionPolicyOverride(RollbackWindow: TimeSpan.FromDays(2)));

    void Because() => _result = _policies.Resolve(_eventStore, _projectionId, false, true);

    [Fact] void should_keep_the_rollback_window_disabled() => _result.RollbackWindow.ShouldEqual(TimeSpan.Zero);
}
