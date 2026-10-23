// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Cratis.Chronicle.Projections.Generations.Policies.for_GenerationPolicies;

public class when_resolving_a_reducer : given.generation_policies
{
    void Because() => _result = _policies.Resolve(_eventStore, _projectionId, false, true);

    [Fact] void should_have_no_rollback_window_by_default() => _result.RollbackWindow.ShouldEqual(TimeSpan.Zero);
}
