// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline;

public class when_a_revision_checked_write_conflicts : given.a_pipeline_whose_write_conflicts
{
    ProjectionEventContext _result;

    async Task Because() => _result = await _pipeline.Handle(SomeEvent());

    [Fact] void should_perform_the_steps_again() => _step.Changesets.Count.ShouldEqual(2);
    [Fact] void should_recompute_from_a_fresh_changeset() => ReferenceEquals(_step.Changesets[0], _step.Changesets[1]).ShouldBeFalse();
    [Fact] void should_not_carry_the_conflicted_changeset_into_the_next_attempt() => ReferenceEquals(_step.Contexts[1].Changeset, _step.Changesets[0]).ShouldBeFalse();
    [Fact] void should_complete_with_the_changeset_of_the_last_attempt() => ReferenceEquals(_result.Changeset, _step.Changesets[1]).ShouldBeTrue();
}
